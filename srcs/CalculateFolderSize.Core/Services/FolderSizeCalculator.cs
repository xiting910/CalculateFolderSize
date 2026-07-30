using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFolderSize.Core.Services;

/// <summary>
/// 文件夹大小计算器
/// </summary>
/// <param name="_options">配置选项</param>
/// <param name="_fileSystem">文件系统抽象</param>
internal sealed class FolderSizeCalculator(CoreOptions _options, IFileSystem _fileSystem) : IFolderSizeCalculator
{
    /// <summary>
    /// 缓存已计算的文件夹大小结果, 避免重复计算
    /// </summary>
    private readonly ConcurrentDictionary<string, FolderSize> _cache = new();

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public int CacheCount => _cache.Count;

    /// <inheritdoc/>
    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <inheritdoc/>
    public Task<FolderSize?> GetFromFolderAsync(string folderPath, IProgress<ProgressReport>? progress = null, CancellationToken token = default)
    {
        return Task.Run(() => GetFromFolder(folderPath, progress, token), token);
    }

    /// <summary>
    /// 从指定文件夹获取大小, 如果文件夹不存在则返回 <see langword="null"/>
    /// </summary>
    /// <param name="path">指定的路径</param>
    /// <param name="progress">进度报告</param>
    /// <param name="token">取消令牌</param>
    /// <returns>文件夹大小结果, 如果文件夹不存在则返回 <see langword="null"/></returns>
    /// <exception cref="OperationCanceledException">当操作被取消时抛出</exception>
    private FolderSize? GetFromFolder(string path, IProgress<ProgressReport>? progress, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return _fileSystem.DirectoryExists(path) ? CalculateSize(path, new(progress), token) : null;
    }

    /// <summary>
    /// 递归计算文件夹大小, 将结果累加到 <paramref name="rootState"/> 中
    /// </summary>
    /// <param name="folderPath">要计算的文件夹路径</param>
    /// <param name="rootState">计算状态</param>
    /// <param name="token">取消令牌</param>
    /// <exception cref="OperationCanceledException">当操作被取消时抛出</exception>
    private FolderSize CalculateSize(string folderPath, State rootState, CancellationToken token)
    {
        if (_cache.TryGetValue(folderPath, out var cachedResult))
        {
            rootState.Add(cachedResult);
            return cachedResult;
        }

        long totalBytes = 0;
        int folderCount = 0, fileCount = 0;
        ConcurrentDictionary<string, Exception> errors = new();

        foreach (var file in _fileSystem.EnumerateFiles(folderPath))
        {
            token.ThrowIfCancellationRequested();
            fileCount++;
            totalBytes += file.Size;
            if (file.Exception is not null)
            {
                _ = errors.TryAdd(file.FullName, file.Exception);
            }
            rootState.AddFile(file.Size);
        }

        var subDirs = _fileSystem.EnumerateDirectories(folderPath);
        var options = new ParallelOptions
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism
        };

        _ = Parallel.ForEach(subDirs, options, subDir =>
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var subDirSize = CalculateSize(subDir.FullName, rootState, token);
                _ = Interlocked.Add(ref totalBytes, subDirSize.TotalBytes);
                _ = Interlocked.Add(ref folderCount, subDirSize.FolderCount + 1);
                _ = Interlocked.Add(ref fileCount, subDirSize.FileCount);
                rootState.AddFolder();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _ = errors.TryAdd(subDir.FullName, ex);
            }
        });

        var result = new FolderSize(folderPath, totalBytes, folderCount, fileCount, errors);
        if (_cache.TryAdd(folderPath, result))
        {
            PropertyChanged?.Invoke(this, new(nameof(CacheCount)));
        }
        return result;
    }

    /// <summary>
    /// 当前计算状态类, 用于报告进度
    /// </summary>
    /// <param name="_progress">进度报告</param>
    private sealed class State(IProgress<ProgressReport>? _progress)
    {
        /// <summary>
        /// 总字节数
        /// </summary>
        private long _totalBytes;

        /// <summary>
        /// 文件夹数量
        /// </summary>
        private int _folderCount;

        /// <summary>
        /// 文件数量
        /// </summary>
        private int _fileCount;

        /// <summary>
        /// 增加指定的文件夹大小到当前状态, 并报告进度
        /// </summary>
        /// <param name="folderSize">文件夹大小</param>
        public void Add(FolderSize folderSize)
        {
            var totalBytes = Interlocked.Add(ref _totalBytes, folderSize.TotalBytes);
            var folderCount = Interlocked.Add(ref _folderCount, folderSize.FolderCount);
            var fileCount = Interlocked.Add(ref _fileCount, folderSize.FileCount);
            _progress?.Report(new(totalBytes, folderCount, fileCount));
        }

        /// <summary>
        /// 增加一个文件的大小到当前状态, 并报告进度
        /// </summary>
        /// <param name="bytes">文件大小</param>
        public void AddFile(long bytes)
        {
            var totalBytes = Interlocked.Add(ref _totalBytes, bytes);
            var fileCount = Interlocked.Increment(ref _fileCount);
            _progress?.Report(new(totalBytes, _folderCount, fileCount));
        }

        /// <summary>
        /// 增加一个文件夹到当前状态, 并报告进度
        /// </summary>
        public void AddFolder()
        {
            var folderCount = Interlocked.Increment(ref _folderCount);
            _progress?.Report(new(_totalBytes, folderCount, _fileCount));
        }
    }
}
