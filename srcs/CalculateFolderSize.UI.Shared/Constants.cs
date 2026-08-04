using System;
using System.IO;
using System.Text.Json;

namespace CalculateFolderSize.UI.Shared;

/// <summary>
/// 常量类
/// </summary>
public static class Constants
{
    /// <summary>
    /// 允许的目录分隔符数组
    /// </summary>
    public static readonly char[] AllowedDirectorySeparators = ['\\', '/'];

    /// <summary>
    /// 缓存的 Json 序列化选项
    /// </summary>
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// 程序数据目录
    /// </summary>
    public static readonly string AppDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        nameof(CalculateFolderSize)
    );

    /// <summary>
    /// 日志文件夹目录
    /// </summary>
    public static readonly string LogsDirectory = Path.Combine(AppDataDirectory, "logs");

    /// <summary>
    /// 最新日志文件路径
    /// </summary>
    public static readonly string LatestLogFilePath = Path.Combine(LogsDirectory, "latest.log");

    /// <summary>
    /// 设置文件路径
    /// </summary>
    public static readonly string SettingsFilePath = Path.Combine(AppDataDirectory, "settings.json");

    /// <summary>
    /// 历史记录文件路径
    /// </summary>
    public static readonly string HistoriesFilePath = Path.Combine(AppDataDirectory, "histories.txt");

    /// <summary>
    /// 获取日志级别的 Key
    /// </summary>
    public const string LogLevelKey = $"{nameof(UI)}:{nameof(Models.UIOptions.Level)}";

    /// <summary>
    /// 日志文件后缀
    /// </summary>
    public const string LogFileExtension = ".log";

    /// <summary>
    /// ViewModel 类型的后缀名常量
    /// </summary>
    public const string ViewModelSuffix = "ViewModel";

    /// <summary>
    /// 允许存在的日志文件数
    /// </summary>
    public const int MaxLogFiles = 5;

    /// <summary>
    /// 允许的最小节流间隔 (毫秒)
    /// </summary>
    public const int MinThrottleIntervalMilliseconds = 100;

    /// <summary>
    /// 允许的最大节流间隔 (毫秒)
    /// </summary>
    public const int MaxThrottleIntervalMilliseconds = 1000;

    /// <summary>
    /// 缓存数界面的合并刷新间隔 (毫秒)
    /// </summary>
    public const int CacheCountRefreshIntervalMilliseconds = 50;

    /// <summary>
    /// 耗时列的刷新间隔 (毫秒)
    /// </summary>
    public const int ElapsedUpdateIntervalMilliseconds = 250;

    /// <summary>
    /// 速度平滑系数 (EMA: 新值权重)
    /// </summary>
    public const double SpeedSmoothingFactor = 0.3;

    /// <summary>
    /// 允许的最小小数位数
    /// </summary>
    public const int MinDecimalPlaces = 0;

    /// <summary>
    /// 允许的最大小数位数
    /// </summary>
    public const int MaxDecimalPlaces = 6;

    /// <summary>
    /// 并行度范围
    /// </summary>
    public const int MinParallelism = 1;

    /// <summary>
    /// 允许的最大并行度
    /// </summary>
    public static readonly int MaxParallelism = Environment.ProcessorCount * 8;
}
