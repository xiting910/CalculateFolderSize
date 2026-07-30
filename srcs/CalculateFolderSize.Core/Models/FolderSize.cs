using System;
using System.Collections.Generic;

namespace CalculateFolderSize.Core.Models;

/// <summary>
/// 表示文件夹大小的不可变记录类型
/// </summary>
/// <param name="Path">文件夹路径</param>
/// <param name="TotalBytes">文件夹总字节数</param>
/// <param name="FolderCount">文件夹数量</param>
/// <param name="FileCount">文件数量</param>
/// <param name="ErrorPaths">错误路径和异常信息的字典</param>
public sealed record FolderSize(
    string Path,
    long TotalBytes,
    int FolderCount,
    int FileCount,
    IReadOnlyDictionary<string, Exception> ErrorPaths
);
