namespace CalculateFolderSize.Core.Models;

/// <summary>
/// 目录条目
/// </summary>
/// <param name="FullName">目录完整路径</param>
/// <param name="Name">目录名称</param>
public readonly record struct DirectoryEntry(string FullName, string Name);
