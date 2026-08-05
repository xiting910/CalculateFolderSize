using CalculateFolderSize.UI.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CalculateFolderSize.UI.Shared.Tests;

public sealed class UIOptionsTests
{
    [Fact]
    public void Constructor_WithValidConfiguration_UsesConfiguredValues()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(UI)}:{nameof(UIOptions.Theme)}"] = nameof(ThemeMode.Dark),
                [$"{nameof(UI)}:{nameof(UIOptions.ThrottleIntervalMilliseconds)}"] = "300",
                [$"{nameof(UI)}:{nameof(UIOptions.ToastDurationSeconds)}"] = "5",
                [$"{nameof(UI)}:{nameof(UIOptions.Level)}"] = nameof(LogLevel.Debug)
            })
            .Build();

        var options = new UIOptions(config);

        Assert.Equal(ThemeMode.Dark, options.Theme);
        Assert.Equal(300, options.ThrottleIntervalMilliseconds);
        Assert.Equal(5, options.ToastDurationSeconds);
        Assert.Equal(LogLevel.Debug, options.Level);
    }

    [Fact]
    public void Constructor_WithInvalidTheme_FallsBackToSystem()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(UI)}:{nameof(UIOptions.Theme)}"] = "NotATheme"
            })
            .Build();

        var options = new UIOptions(config);

        Assert.Equal(ThemeMode.System, options.Theme);
    }

    [Theory]
    [InlineData("50", 100)]
    [InlineData("5000", 1000)]
    public void Constructor_WithOutOfRangeThrottle_ClampsToBounds(string value, int expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(UI)}:{nameof(UIOptions.ThrottleIntervalMilliseconds)}"] = value
            })
            .Build();

        var options = new UIOptions(config);

        Assert.Equal(expected, options.ThrottleIntervalMilliseconds);
    }

    [Fact]
    public void Constructor_WithInvalidThrottle_FallsBackToDefault()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(UI)}:{nameof(UIOptions.ThrottleIntervalMilliseconds)}"] = "NotANumber"
            })
            .Build();

        var options = new UIOptions(config);

        Assert.Equal(Constants.DefaultThrottleIntervalMilliseconds, options.ThrottleIntervalMilliseconds);
    }

    [Theory]
    [InlineData("-1", 0)]
    [InlineData("15", 10)]
    public void Constructor_WithOutOfRangeToastDuration_ClampsToBounds(string value, double expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(UI)}:{nameof(UIOptions.ToastDurationSeconds)}"] = value
            })
            .Build();

        var options = new UIOptions(config);

        Assert.Equal(expected, options.ToastDurationSeconds);
    }

    [Fact]
    public void Constructor_WithInvalidToastDuration_FallsBackToDefault()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(UI)}:{nameof(UIOptions.ToastDurationSeconds)}"] = "NotANumber"
            })
            .Build();

        var options = new UIOptions(config);

        Assert.Equal(Constants.DefaultToastDurationSeconds, options.ToastDurationSeconds);
    }

    [Fact]
    public void Constructor_WithInvalidLevel_FallsBackToInformation()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(UI)}:{nameof(UIOptions.Level)}"] = "NotALevel"
            })
            .Build();

        var options = new UIOptions(config);

        Assert.Equal(LogLevel.Information, options.Level);
    }

    [Fact]
    public void Constructor_WithMissingSection_FallsBackToDefaults()
    {
        var config = new ConfigurationBuilder().Build();

        var options = new UIOptions(config);

        Assert.Equal(ThemeMode.System, options.Theme);
        Assert.Equal(Constants.DefaultThrottleIntervalMilliseconds, options.ThrottleIntervalMilliseconds);
        Assert.Equal(Constants.DefaultToastDurationSeconds, options.ToastDurationSeconds);
        Assert.Equal(LogLevel.Information, options.Level);
    }
}
