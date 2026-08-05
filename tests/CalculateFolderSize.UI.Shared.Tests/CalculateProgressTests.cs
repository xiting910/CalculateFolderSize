using CalculateFolderSize.Core.Models;
using CalculateFolderSize.UI.Shared.Models;
using CalculateFolderSize.UI.Shared.Services;
using Microsoft.Extensions.Logging;

namespace CalculateFolderSize.UI.Shared.Tests;

public sealed class CalculateProgressTests
{
    private sealed class FakeTimeProvider(DateTimeOffset startTime) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = startTime;

        public override DateTimeOffset GetUtcNow()
        {
            return Now;
        }
    }

    private static CalculateProgress CreateProgress(FakeTimeProvider timeProvider)
    {
        return new(new UIOptions(ThemeMode.System, 200, 3, LogLevel.Information), timeProvider);
    }

    [Fact]
    public void Report_WithinThrottleInterval_DoesNotRaiseEvent()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var progress = CreateProgress(timeProvider);
        var raisedCount = 0;
        progress.ProgressUpdated += (_, _) => raisedCount++;

        progress.Report(new(1000, 1, 2));

        Assert.Equal(0, raisedCount);
    }

    [Fact]
    public void Report_AfterThrottleInterval_RaisesEventWithInstantSpeed()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var progress = CreateProgress(timeProvider);
        CalculateProgressUpdateEventArgs? args = null;
        progress.ProgressUpdated += (_, e) => args = e;

        timeProvider.Now = DateTimeOffset.UnixEpoch.AddMilliseconds(200);
        progress.Report(new(1000, 1, 2));

        Assert.NotNull(args);
        Assert.Equal(new ProgressReport(1000, 1, 2), args.ProgressReport);
        Assert.Equal(5000, args.SpeedBytesPerSecond, 0.001);
    }

    [Fact]
    public void Report_SecondUpdateAfterThrottle_AppliesEmaSmoothing()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var progress = CreateProgress(timeProvider);
        var speeds = new List<double>();
        progress.ProgressUpdated += (_, e) => speeds.Add(e.SpeedBytesPerSecond);

        timeProvider.Now = DateTimeOffset.UnixEpoch.AddMilliseconds(200);
        progress.Report(new(1000, 0, 0));
        timeProvider.Now = DateTimeOffset.UnixEpoch.AddMilliseconds(400);
        progress.Report(new(3000, 0, 0));

        Assert.Equal(2, speeds.Count);
        Assert.Equal(5000, speeds[0], 0.001);
        Assert.Equal((5000 * 0.7) + (10000 * 0.3), speeds[1], 0.001);
    }

    [Fact]
    public void Report_EventAfterThrottle_CarriesLatestReport()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var progress = CreateProgress(timeProvider);
        CalculateProgressUpdateEventArgs? args = null;
        progress.ProgressUpdated += (_, e) => args = e;

        timeProvider.Now = DateTimeOffset.UnixEpoch.AddMilliseconds(200);
        progress.Report(new(1000, 1, 1));
        timeProvider.Now = DateTimeOffset.UnixEpoch.AddMilliseconds(250);
        progress.Report(new(1500, 2, 2));
        timeProvider.Now = DateTimeOffset.UnixEpoch.AddMilliseconds(450);
        progress.Report(new(2500, 3, 3));

        Assert.NotNull(args);
        Assert.Equal(new ProgressReport(2500, 3, 3), args.ProgressReport);
        Assert.Equal((5000 * 0.7) + (6000 * 0.3), args.SpeedBytesPerSecond, 0.001);
    }

    [Fact]
    public void Complete_RaisesEventWithOverallSpeed()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var progress = CreateProgress(timeProvider);
        CalculateProgressUpdateEventArgs? args = null;
        progress.ProgressUpdated += (_, e) => args = e;

        timeProvider.Now = DateTimeOffset.UnixEpoch.AddMilliseconds(200);
        progress.Report(new(1000, 1, 2));
        timeProvider.Now = DateTimeOffset.UnixEpoch.AddMilliseconds(400);
        progress.Complete();

        Assert.NotNull(args);
        Assert.Equal(new ProgressReport(1000, 1, 2), args.ProgressReport);
        Assert.Equal(2500, args.SpeedBytesPerSecond, 0.001);
    }

    [Fact]
    public void ProgressUpdated_EventSender_IsProgressInstance()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var progress = CreateProgress(timeProvider);
        object? sender = null;
        progress.ProgressUpdated += (s, _) => sender = s;

        timeProvider.Now = DateTimeOffset.UnixEpoch.AddMilliseconds(200);
        progress.Report(new(100, 0, 0));

        Assert.Same(progress, sender);
    }
}
