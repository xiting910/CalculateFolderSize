using System;

namespace CalculateFolderSize.Core.Models;

/// <summary>
/// 文件子项类
/// </summary>
/// <param name="Path">文件子项路径</param>
/// <param name="Name">文件子项名称</param>
/// <param name="Size">文件子项总字节数</param>
/// <param name="Exception">枚举文件时发生的异常</param>
public sealed record FileChild(
    string Path,
    string Name,
    long Size,
    Exception? Exception
) : FolderChild(Path, Name, Size)
{
    /// <summary>
    /// 使用 <see cref="FileEntry"/> 的构造函数
    /// </summary>
    /// <param name="fileEntry">文件条目</param>
    public FileChild(FileEntry fileEntry) : this(fileEntry.FullName, fileEntry.Name, fileEntry.Size, fileEntry.Exception) { }
}
