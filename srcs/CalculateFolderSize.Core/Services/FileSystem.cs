using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CalculateFolderSize.Core.Services;

/// <summary>
/// 文件系统
/// </summary>
internal sealed class FileSystem : IFileSystem
{
    /// <inheritdoc />
    public bool DirectoryExists([NotNullWhen(true)] string? path)
    {
        return Directory.Exists(path);
    }

    /// <inheritdoc />
    public IEnumerable<FileEntry> EnumerateFiles(string directoryPath)
    {
        var directoryInfo = new DirectoryInfo(directoryPath);
        if (!directoryInfo.Exists)
        {
            throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");
        }

        long size;
        Exception? exception;
        foreach (var fileInfo in directoryInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
        {
            try
            {
                size = fileInfo.Length;
                exception = null;
            }
            catch (Exception ex)
            {
                size = 0;
                exception = ex;
            }
            yield return new(fileInfo.FullName, size, exception);
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> EnumerateDirectories(string directoryPath)
    {
        var directoryInfo = new DirectoryInfo(directoryPath);
        if (!directoryInfo.Exists)
        {
            throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");
        }

        foreach (var subDirInfo in directoryInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
        {
            if ((subDirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                continue;
            }
            yield return subDirInfo.FullName;
        }
    }
}
