using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace CalculateFolderSize.UI.Shared.Models;

/// <summary>
/// UI 层配置选项
/// </summary>
/// <param name="Level">日志级别</param>
/// <param name="Theme">主题模式</param>
/// <param name="ThrottleIntervalMilliseconds">进度报告的节流间隔, 单位为毫秒</param>
public sealed record UIOptions(LogLevel Level, ThemeMode Theme, int ThrottleIntervalMilliseconds)
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

        if (!Enum.TryParse<LogLevel>(section[nameof(Level)], out var level))
        {
            level = LogLevel.Information;
        }

        if (!Enum.TryParse<ThemeMode>(section[nameof(Theme)], out var theme))
        {
            theme = ThemeMode.System;
        }

        if (!int.TryParse(section[nameof(ThrottleIntervalMilliseconds)], out var throttleIntervalMilliseconds)
            || throttleIntervalMilliseconds < Constants.MinThrottleIntervalMilliseconds)
        {
            throttleIntervalMilliseconds = Constants.MinThrottleIntervalMilliseconds;
        }

        return new(level, theme, throttleIntervalMilliseconds);
    }
}
