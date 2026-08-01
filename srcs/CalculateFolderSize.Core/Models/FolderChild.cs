namespace CalculateFolderSize.Core.Models;

/// <summary>
/// 表示文件夹子项的抽象记录类型
/// </summary>
/// <param name="Path">文件夹子项路径</param>
/// <param name="Name">文件夹子项名称</param>
/// <param name="Size">文件夹子项总字节数</param>
public abstract record FolderChild(string Path, string Name, long Size);
