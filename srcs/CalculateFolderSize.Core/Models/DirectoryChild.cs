using System;
using System.Collections.Generic;

namespace CalculateFolderSize.Core.Models;

/// <summary>
/// 文件夹子项类
/// </summary>
/// <param name="Path">文件夹子项路径</param>
/// <param name="Name">文件夹子项名称</param>
/// <param name="Size">文件夹子项总字节数</param>
/// <param name="FolderCount">文件夹数量</param>
/// <param name="FileCount">文件数量</param>
/// <param name="ErrorPaths">错误路径和异常信息的字典</param>
public sealed record DirectoryChild(
    string Path,
    string Name,
    long Size,
    int FolderCount,
    int FileCount,
    IReadOnlyDictionary<string, Exception> ErrorPaths
) : FolderChild(Path, Name, Size)
{
    /// <summary>
    /// 使用 <see cref="FolderSize"/> 的构造函数
    /// </summary>
    /// <param name="name">文件夹子项名称</param>
    /// <param name="folderSize">文件夹大小</param>
    public DirectoryChild(string name, FolderSize folderSize) : this(folderSize.Path, name, folderSize.TotalBytes, folderSize.FolderCount, folderSize.FileCount, folderSize.ErrorPaths) { }
}
