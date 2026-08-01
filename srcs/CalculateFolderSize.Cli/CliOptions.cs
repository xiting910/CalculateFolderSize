using CalculateFolderSize.Core.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace CalculateFolderSize.Cli;

/// <summary>
/// Cli 项目的配置类
/// </summary>
/// <param name="SizeStringLength">文件大小字符串的长度, 用于对齐输出</param>
/// <param name="DirectorySeparator">要使用的目标目录分隔符</param>
/// <param name="ReplacedSeparator">被替换的目录分隔符, 用于在输出中替换目录分隔符</param>
/// <param name="ExitCommand">退出命令</param>
/// <param name="ClearCacheCommand">清除缓存命令</param>
public sealed record CliOptions(
    int SizeStringLength,
    char DirectorySeparator,
    char ReplacedSeparator,
    string ExitCommand,
    string ClearCacheCommand)
{
    /// <summary>
    /// 连续的目录分隔符, 用于在字符串中查找和替换连续的目录分隔符
    /// </summary>
    public string ConsecutiveDirectorySeparators { get; } = new(DirectorySeparator, 2);

    /// <summary>
    /// 使用 <see cref="IConfiguration"/> 和 <see cref="CoreOptions"/> 的构造函数
    /// </summary>
    /// <param name="configuration">要使用的 <see cref="IConfiguration"/> 实例</param>
    /// <param name="coreOptions">要使用的 <see cref="CoreOptions"/> 实例</param>
    public CliOptions(IConfiguration configuration, CoreOptions coreOptions) : this(Create(configuration, coreOptions)) { }

    /// <summary>
    /// 使用 <see cref="CliOptions"/> 和 <see cref="CoreOptions"/> 创建 <see cref="CliOptions"/> 的静态方法
    /// </summary>
    /// <param name="configuration">要使用的 <see cref="IConfiguration"/> 实例</param>
    /// <param name="coreOptions">要使用的 <see cref="CoreOptions"/> 实例</param>
    /// <returns>创建的 <see cref="CliOptions"/> 实例</returns>
    private static CliOptions Create(IConfiguration configuration, CoreOptions coreOptions)
    {
        var section = configuration.GetSection(nameof(Cli));

        if (!int.TryParse(section[nameof(SizeStringLength)], out var length) || length <= 0)
        {
            length = 4 + 1 + coreOptions.DecimalPlaces + 2;
        }

        if (!char.TryParse(section[nameof(DirectorySeparator)], out var separator) || !IsValid(separator))
        {
            separator = '\\';
        }

        if (!char.TryParse(section[nameof(ReplacedSeparator)], out var replaced) || !IsValid(replaced))
        {
            replaced = '/';
        }

        var exitCommand = section[nameof(ExitCommand)];
        if (string.IsNullOrWhiteSpace(exitCommand))
        {
            exitCommand = "exit";
        }

        var clearCacheCommand = section[nameof(ClearCacheCommand)];
        if (string.IsNullOrWhiteSpace(clearCacheCommand))
        {
            clearCacheCommand = "clearcache";
        }

        return new(length, separator, replaced, exitCommand, clearCacheCommand);
    }

    /// <summary>
    /// 验证目录分隔符是否有效
    /// </summary>
    /// <param name="separator">要验证的目录分隔符</param>
    /// <returns><see langword="true"/> 如果分隔符有效, 否则 <see langword="false"/></returns>
    private static bool IsValid(char separator)
    {
        return Array.Exists(AllowedSeparators, s => s == separator);
    }

    /// <summary>
    /// 允许的目录分隔符数组, 用于验证配置文件中的目录分隔符是否有效
    /// </summary>
    private static readonly char[] AllowedSeparators = ['\\', '/'];
}
