using CalculateFolderSize.Core.Models;
using System.Collections.Generic;

namespace CalculateFolderSize.Core.Interfaces;

/// <summary>
/// 文件系统接口
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// 检查指定路径是否存在且为文件夹
    /// </summary>
    /// <param name="path">要检查的路径</param>
    /// <returns><see langword="true"/> 如果路径存在且为文件夹; 否则为 <see langword="false"/></returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// 枚举指定目录下的所有文件
    /// </summary>
    /// <param name="directoryPath">要枚举的目录路径</param>
    /// <returns>文件条目集合</returns>
    /// <exception cref="System.IO.DirectoryNotFoundException">指定的目录不存在</exception>
    /// <exception cref="System.UnauthorizedAccessException">没有权限访问指定的目录</exception>
    IEnumerable<FileEntry> EnumerateFiles(string directoryPath);

    /// <summary>
    /// 枚举指定目录下的所有子目录
    /// </summary>
    /// <param name="directoryPath">要枚举的目录路径</param>
    /// <returns>子目录条目集合</returns>
    /// <exception cref="System.IO.DirectoryNotFoundException">指定的目录不存在</exception>
    /// <exception cref="System.UnauthorizedAccessException">没有权限访问指定的目录</exception>
    IEnumerable<DirectoryEntry> EnumerateDirectories(string directoryPath);
}
