using Microsoft.Extensions.Logging;
using System;

namespace CalculateFolderSize.Core.Services;

/// <summary>
/// 文件夹大小计算器的日志事件定义, 实现由 <see cref="LoggerMessageAttribute"/> 源生成器生成
/// </summary>
internal sealed partial class FolderSizeCalculator
{
    /// <summary>
    /// 记录扫描开始
    /// </summary>
    /// <param name="path">文件夹路径</param>
    [LoggerMessage(EventId = 1, EventName = "ScanStarted", Level = LogLevel.Information,
        Message = "Scan started: {Path}")]
    private partial void LogScanStarted(string path);

    /// <summary>
    /// 记录扫描完成
    /// </summary>
    /// <param name="path">文件夹路径</param>
    /// <param name="totalBytes">总字节数</param>
    /// <param name="fileCount">文件数量</param>
    /// <param name="folderCount">文件夹数量</param>
    [LoggerMessage(EventId = 2, EventName = "ScanCompleted", Level = LogLevel.Information,
        Message = "Scan completed: {Path}, TotalBytes={TotalBytes}, Files={FileCount}, Folders={FolderCount}")]
    private partial void LogScanCompleted(string path, long totalBytes, int fileCount, int folderCount);

    /// <summary>
    /// 记录目录不存在
    /// </summary>
    /// <param name="path">文件夹路径</param>
    [LoggerMessage(EventId = 3, EventName = "DirectoryNotFound", Level = LogLevel.Information,
        Message = "Directory not found: {Path}")]
    private partial void LogDirectoryNotFound(string path);

    /// <summary>
    /// 记录缓存命中
    /// </summary>
    /// <param name="path">文件夹路径</param>
    [LoggerMessage(EventId = 4, EventName = "CacheHit", Level = LogLevel.Debug,
        Message = "Cache hit: {Path}")]
    private partial void LogCacheHit(string path);

    /// <summary>
    /// 记录子目录计算完成
    /// </summary>
    /// <param name="path">子目录路径</param>
    /// <param name="totalBytes">总字节数</param>
    /// <param name="fileCount">文件数量</param>
    /// <param name="folderCount">文件夹数量</param>
    [LoggerMessage(EventId = 5, EventName = "DirectoryCalculated", Level = LogLevel.Debug,
        Message = "Calculated size: {Path}, TotalBytes={TotalBytes}, Files={FileCount}, Folders={FolderCount}")]
    private partial void LogDirectoryCalculated(string path, long totalBytes, int fileCount, int folderCount);

    /// <summary>
    /// 记录获取文件大小失败
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="exception">异常</param>
    [LoggerMessage(EventId = 6, EventName = "FileSizeFailed", Level = LogLevel.Debug,
        Message = "Failed to get file size: {Path}")]
    private partial void LogFileSizeFailed(string path, Exception exception);

    /// <summary>
    /// 记录计算子目录失败
    /// </summary>
    /// <param name="path">子目录路径</param>
    /// <param name="exception">异常</param>
    [LoggerMessage(EventId = 7, EventName = "SubDirectoryFailed", Level = LogLevel.Debug,
        Message = "Failed to compute subdirectory: {Path}")]
    private partial void LogSubDirectoryFailed(string path, Exception exception);

    /// <summary>
    /// 记录缓存清理
    /// </summary>
    /// <param name="results">结果缓存条目数</param>
    /// <param name="children">子项缓存条目数</param>
    [LoggerMessage(EventId = 8, EventName = "CacheCleared", Level = LogLevel.Information,
        Message = "Cache cleared: Results={Results}, Children={Children}")]
    private partial void LogCacheCleared(int results, int children);

    /// <summary>
    /// 记录扫描被取消
    /// </summary>
    /// <param name="path">文件夹路径</param>
    [LoggerMessage(EventId = 9, EventName = "ScanCanceled", Level = LogLevel.Debug,
        Message = "Scan canceled: {Path}")]
    private partial void LogScanCanceled(string path);

    /// <summary>
    /// 记录子项缓存写入失败
    /// </summary>
    /// <param name="path">文件夹路径</param>
    [LoggerMessage(EventId = 10, EventName = "ChildrenCacheFailed", Level = LogLevel.Debug,
        Message = "Failed to cache children: {Path}")]
    private partial void LogChildrenCacheFailed(string path);
}
