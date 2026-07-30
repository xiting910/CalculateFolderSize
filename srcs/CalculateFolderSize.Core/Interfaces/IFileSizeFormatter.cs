using System;

namespace CalculateFolderSize.Core.Interfaces;

/// <summary>
/// 文件大小格式化器接口
/// </summary>
public interface IFileSizeFormatter
{
    /// <summary>
    /// 将字节大小格式化为可读的文件大小字符串
    /// </summary>
    /// <param name="bytes">字节大小</param>
    /// <returns>可读的文件大小字符串</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="bytes"/>为负数时抛出</exception>
    string Format(long bytes);
}
