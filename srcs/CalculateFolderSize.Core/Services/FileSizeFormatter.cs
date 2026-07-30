using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using System;

namespace CalculateFolderSize.Core.Services;

/// <summary>
/// 文件大小格式化器
/// </summary>
/// <param name="_options">配置选项</param>
internal sealed class FileSizeFormatter(CoreOptions _options) : IFileSizeFormatter
{
    /// <summary>
    /// 字节单位换算基数
    /// </summary>
    private const int BytesPerKilo = 1024;

    /// <summary>
    /// 文件大小单位后缀
    /// </summary>
    private static readonly string[] SizeSuffixes = [" B", "KB", "MB", "GB", "TB", "PB", "EB"];

    /// <inheritdoc />
    public string Format(long bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes, nameof(bytes));
        if (bytes == 0) { return "0" + SizeSuffixes[0]; }

        var magnitude = 0;
        var size = (decimal)bytes;
        while (size >= BytesPerKilo && magnitude < SizeSuffixes.Length - 1)
        {
            size /= BytesPerKilo;
            magnitude++;
        }

        var format = "0";
        if (_options.DecimalPlaces > 0)
        {
            format += "." + new string('#', _options.DecimalPlaces);
        }
        return size.ToString(format) + SizeSuffixes[magnitude];
    }
}
