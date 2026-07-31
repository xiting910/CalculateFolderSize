using CalculateFolderSize.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CalculateFolderSize.Core.Interfaces;

/// <summary>
/// 文件系统接口
/// </summary>
internal interface IFileSystem
{
    /// <summary>
    /// 检查指定路径是否存在且为文件夹
    /// </summary>
    /// <param name="path">要检查的路径</param>
    /// <returns><see langword="true"/> 如果路径存在且为文件夹; 否则为 <see langword="false"/></returns>
    bool DirectoryExists([NotNullWhen(true)] string? path);

    /// <summary>
    /// 枚举指定目录下的所有文件
    /// </summary>
    /// <param name="directoryPath">要枚举的目录路径</param>
    /// <returns>文件条目集合</returns>
    /// <exception cref="DirectoryNotFoundException">指定的目录不存在</exception>
    /// <exception cref="UnauthorizedAccessException">没有权限访问指定的目录</exception>
    IEnumerable<FileEntry> EnumerateFiles(string directoryPath);

    /// <summary>
    /// 枚举指定目录下的所有子目录
    /// </summary>
    /// <param name="directoryPath">要枚举的目录路径</param>
    /// <returns>子目录条目集合</returns>
    /// <exception cref="DirectoryNotFoundException">指定的目录不存在</exception>
    /// <exception cref="UnauthorizedAccessException">没有权限访问指定的目录</exception>
    IEnumerable<string> EnumerateDirectories(string directoryPath);
}
