using Microsoft.Extensions.Logging;
using System;

namespace CalculateFolderSize.Core.Services;

/// <summary>
/// 文件夹大小计算器的日志事件定义, 实现由 <see cref="LoggerMessageAttribute"/> 源生成器生成
/// </summary>
internal sealed partial class FolderSizeCalculator
{
    /// <summary>
    /// 记录目录不存在
    /// </summary>
    /// <param name="path">文件夹路径</param>
    [LoggerMessage(
        EventId = 1,
        EventName = "DirectoryNotFound",
        Level = LogLevel.Information,
        Message = "目录未找到: {Path}"
    )]
    private partial void LogDirectoryNotFound(string path);

    /// <summary>
    /// 记录扫描开始
    /// </summary>
    /// <param name="path">文件夹路径</param>
    [LoggerMessage(
        EventId = 2,
        EventName = "ScanStarted",
        Level = LogLevel.Information,
        Message = "扫描开始: {Path}"
    )]
    private partial void LogScanStarted(string path);

    /// <summary>
    /// 记录缓存命中
    /// </summary>
    /// <param name="path">文件夹路径</param>
    [LoggerMessage(
        EventId = 3,
        EventName = "CacheHit",
        Level = LogLevel.Debug,
        Message = "缓存命中: {Path}"
    )]
    private partial void LogCacheHit(string path);

    /// <summary>
    /// 记录获取文件大小失败
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="exception">异常</param>
    [LoggerMessage(
        EventId = 4,
        EventName = "FileSizeFailed",
        Level = LogLevel.Information,
        Message = "获取文件大小失败: {Path}"
    )]
    private partial void LogFileSizeFailed(string path, Exception exception);

    /// <summary>
    /// 记录计算子目录失败
    /// </summary>
    /// <param name="path">子目录路径</param>
    /// <param name="exception">异常</param>
    [LoggerMessage(
        EventId = 5,
        EventName = "SubDirectoryFailed",
        Level = LogLevel.Information,
        Message = "计算子目录失败: {Path}"
    )]
    private partial void LogSubDirectoryFailed(string path, Exception exception);

    /// <summary>
    /// 记录子目录计算完成
    /// </summary>
    /// <param name="path">子目录路径</param>
    /// <param name="totalBytes">总字节数</param>
    /// <param name="fileCount">文件数量</param>
    /// <param name="folderCount">文件夹数量</param>
    [LoggerMessage(
        EventId = 6,
        EventName = "DirectoryCalculated",
        Level = LogLevel.Debug,
        Message = "子目录计算完成: {Path}: 总字节数={TotalBytes}, 文件数={FileCount}, 文件夹数={FolderCount}"
    )]
    private partial void LogDirectoryCalculated(string path, long totalBytes, int fileCount, int folderCount);

    /// <summary>
    /// 记录子项缓存写入失败
    /// </summary>
    /// <param name="path">文件夹路径</param>
    [LoggerMessage(
        EventId = 7,
        EventName = "ChildrenCacheFailed",
        Level = LogLevel.Information,
        Message = "缓存子项失败: {Path}"
    )]
    private partial void LogChildrenCacheFailed(string path);

    /// <summary>
    /// 记录扫描完成
    /// </summary>
    /// <param name="path">文件夹路径</param>
    /// <param name="totalBytes">总字节数</param>
    /// <param name="fileCount">文件数量</param>
    /// <param name="folderCount">文件夹数量</param>
    [LoggerMessage(
        EventId = 8,
        EventName = "ScanCompleted",
        Level = LogLevel.Information,
        Message = "扫描完成: {Path}, 总字节数={TotalBytes}, 文件数={FileCount}, 文件夹数={FolderCount}"
    )]
    private partial void LogScanCompleted(string path, long totalBytes, int fileCount, int folderCount);

    /// <summary>
    /// 记录扫描被取消
    /// </summary>
    /// <param name="path">文件夹路径</param>
    [LoggerMessage(
        EventId = 9,
        EventName = "ScanCanceled",
        Level = LogLevel.Debug,
        Message = "扫描被取消: {Path}"
    )]
    private partial void LogScanCanceled(string path);

    /// <summary>
    /// 记录缓存清理被取消
    /// </summary>
    /// <param name="activeCalculations">当前活动计算数量</param>
    [LoggerMessage(
        EventId = 10,
        EventName = "CacheClearedCancelled",
        Level = LogLevel.Information,
        Message = "缓存清理被取消, 当前活动计算数量: {ActiveCalculations}"
    )]
    private partial void LogCacheClearedCancelled(int activeCalculations);

    /// <summary>
    /// 记录缓存清理
    /// </summary>
    [LoggerMessage(
        EventId = 11,
        EventName = "CacheCleared",
        Level = LogLevel.Information,
        Message = "成功缓存清理"
    )]
    private partial void LogCacheCleared();
}
