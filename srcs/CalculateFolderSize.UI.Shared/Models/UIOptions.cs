using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace CalculateFolderSize.UI.Shared.Models;

/// <summary>
/// UI 层配置选项
/// </summary>
/// <param name="Theme">主题模式</param>
/// <param name="ThrottleIntervalMilliseconds">进度报告的节流间隔, 单位为毫秒</param>
/// <param name="ToastDurationSeconds">Toast 提示的显示时间, 单位为秒</param>
/// <param name="Level">日志级别</param>
public sealed record UIOptions(
    ThemeMode Theme,
    int ThrottleIntervalMilliseconds,
    double ToastDurationSeconds,
    LogLevel Level)
{
    /// <summary>
    /// 使用 <see cref="IConfiguration"/> 的构造函数
    /// </summary>
    /// <param name="configuration">要使用的 <see cref="IConfiguration"/> 实例</param>
    public UIOptions(IConfiguration configuration) : this(Create(configuration)) { }

    /// <summary>
    /// 从配置创建 <see cref="UIOptions"/> 实例
    /// </summary>
    /// <param name="configuration">要使用的 <see cref="IConfiguration"/> 实例</param>
    /// <returns>创建的 <see cref="UIOptions"/> 实例</returns>
    private static UIOptions Create(IConfiguration configuration)
    {
        var section = configuration.GetSection(nameof(UI));

        if (!Enum.TryParse<ThemeMode>(section[nameof(Theme)], out var theme))
        {
            theme = ThemeMode.System;
        }

        if (int.TryParse(section[nameof(ThrottleIntervalMilliseconds)], out var tIMs))
        {
            tIMs = tIMs switch
            {
                < Constants.MinThrottleIntervalMilliseconds => Constants.MinThrottleIntervalMilliseconds,
                > Constants.MaxThrottleIntervalMilliseconds => Constants.MaxThrottleIntervalMilliseconds,
                _ => tIMs
            };
        }
        else
        {
            tIMs = Constants.DefaultThrottleIntervalMilliseconds;
        }

        if (double.TryParse(section[nameof(ToastDurationSeconds)], out var toastDurationSeconds))
        {
            toastDurationSeconds = toastDurationSeconds switch
            {
                < Constants.MinToastDurationSeconds => Constants.MinToastDurationSeconds,
                > Constants.MaxToastDurationSeconds => Constants.MaxToastDurationSeconds,
                _ => toastDurationSeconds
            };
        }
        else
        {
            toastDurationSeconds = Constants.DefaultToastDurationSeconds;
        }

        if (!Enum.TryParse<LogLevel>(section[nameof(Level)], out var level))
        {
            level = LogLevel.Information;
        }

        return new(theme, tIMs, toastDurationSeconds, level);
    }
}
