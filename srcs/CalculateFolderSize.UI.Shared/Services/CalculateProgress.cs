using CalculateFolderSize.Core.Models;
using CalculateFolderSize.UI.Shared.Interfaces;
using CalculateFolderSize.UI.Shared.Models;
using System;
using System.Threading;

namespace CalculateFolderSize.UI.Shared.Services;

/// <summary>
/// 计算进度
/// </summary>
/// <param name="_uiOptions">UI 层配置选项</param>
/// <param name="_timeProvider">时间提供器</param>
internal sealed class CalculateProgress(UIOptions _uiOptions, TimeProvider _timeProvider) : ICalculateProgress
{
    /// <summary>
    /// 节流间隔
    /// </summary>
    private readonly TimeSpan _throttleInterval =
        TimeSpan.FromMilliseconds(_uiOptions.ThrottleIntervalMilliseconds);

    /// <summary>
    /// 锁对象, 用于确保线程安全
    /// </summary>
    private readonly Lock _lock = new();

    /// <summary>
    /// 开始计算的时间
    /// </summary>
    private readonly DateTimeOffset _startTime = _timeProvider.GetUtcNow();

    /// <summary>
    /// 上一次更新的时间
    /// </summary>
    private DateTimeOffset _lastUpdatedTime = _timeProvider.GetUtcNow();

    /// <summary>
    /// 上一次更新时的进度报告
    /// </summary>
    private ProgressReport _lastUpdatedReport = new(0, 0, 0);

    /// <summary>
    /// 最新报告的进度
    /// </summary>
    private ProgressReport _latestReport = new(0, 0, 0);

    /// <summary>
    /// 上次更新的计算速度, 第一次更新时为 <see langword="null"/>
    /// </summary>
    private double? _lastSpeed;

    /// <inheritdoc/>
    public event EventHandler<CalculateProgressUpdateEventArgs>? ProgressUpdated;

    /// <inheritdoc/>
    public void Complete()
    {
        var now = _timeProvider.GetUtcNow();
        lock (_lock)
        {
            var interval = now - _startTime;
            var speed = _latestReport.BytesSoFar / interval.TotalSeconds;
            _lastSpeed = speed;
            _lastUpdatedTime = now;
            _lastUpdatedReport = _latestReport;
        }
        ProgressUpdated?.Invoke(this, new(_latestReport, _lastSpeed.Value));
    }

    /// <inheritdoc/>
    public void Report(ProgressReport value)
    {
        double? speed = null;
        var now = _timeProvider.GetUtcNow();
        lock (_lock)
        {
            _latestReport = value;
            if (now - _lastUpdatedTime >= _throttleInterval)
            {
                var bytes = value.BytesSoFar - _lastUpdatedReport.BytesSoFar;
                var interval = now - _lastUpdatedTime;
                var instantSpeed = bytes / interval.TotalSeconds;
                speed = _lastSpeed = _lastSpeed is double lastSpeed
                    ? (lastSpeed * (1 - Constants.SpeedSmoothingFactor))
                    + (instantSpeed * Constants.SpeedSmoothingFactor)
                    : instantSpeed;

                _lastUpdatedTime = now;
                _lastUpdatedReport = value;
            }
        }
        if (speed.HasValue)
        {
            ProgressUpdated?.Invoke(this, new(_latestReport, speed.Value));
        }
    }
}
