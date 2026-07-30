using System;

namespace CalculateFolderSize.Core.Models;

/// <summary>
/// 文件条目
/// </summary>
/// <param name="FullName">文件完整路径</param>
/// <param name="Size">文件字节大小</param>
/// <param name="Exception">获取文件大小时发生的异常</param>
public readonly record struct FileEntry(string FullName, long Size, Exception? Exception);
