using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
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
    /// 当前对象是否已被释放
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 缓存已计算的文件夹大小结果, 避免重复计算
    /// </summary>
    private readonly ConcurrentDictionary<string, FolderSize> _cache = new(_options.PathComparer);

    /// <summary>
    /// 用于对每个文件夹路径进行锁定, 避免并发计算同一文件夹
    /// </summary>
    private readonly ConcurrentDictionary<string, Lazy<SemaphoreSlim>> _pathLocks = new(_options.PathComparer);

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public int CacheCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _cache.Count;
        }
    }

    /// <inheritdoc/>
    public void ClearCache()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Clear();
        PropertyChanged?.Invoke(this, new(nameof(CacheCount)));
    }

    /// <inheritdoc/>
    public FolderSize? GetFromFolder(
        string folderPath,
        IProgress<ProgressReport>? progress = null,
        CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        token.ThrowIfCancellationRequested();
        if (!_fileSystem.DirectoryExists(folderPath)) { return null; }

        var state = progress is null ? null : new State(progress);
        return CalculateSize(folderPath, state, token);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Clear();
        }
    }

    /// <summary>
    /// 不检查对象是否已被释放, 不触发 <see cref="PropertyChanged"/> 事件, 直接清理缓存和锁定对象
    /// </summary>
    private void Clear()
    {
        foreach (var semaphoreSlim in _pathLocks.Values.Where(l => l.IsValueCreated).Select(l => l.Value))
        {
            semaphoreSlim.Dispose();
        }
        _pathLocks.Clear();
        _cache.Clear();
    }

    /// <summary>
    /// 递归计算文件夹大小, 将结果累加到 <paramref name="rootState"/> 中
    /// </summary>
    /// <param name="folderPath">要计算的文件夹路径</param>
    /// <param name="rootState">计算状态</param>
    /// <param name="token">取消令牌</param>
    /// <exception cref="ObjectDisposedException">当对象已被释放时抛出</exception>
    /// <exception cref="OperationCanceledException">当操作被取消时抛出</exception>
    private FolderSize CalculateSize(string folderPath, State? rootState, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_cache.TryGetValue(folderPath, out var cachedResult))
        {
            rootState?.Add(cachedResult);
            return cachedResult;
        }

        var semaphoreSlim = _pathLocks.GetOrAdd(folderPath, _ => new(() => new(1, 1))).Value;
        semaphoreSlim.Wait(token);

        try
        {
            if (_cache.TryGetValue(folderPath, out cachedResult))
            {
                rootState?.Add(cachedResult);
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
                rootState?.AddFile(file.Size);
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
                    var subDirSize = CalculateSize(subDir, rootState, token);
                    _ = Interlocked.Add(ref totalBytes, subDirSize.TotalBytes);
                    _ = Interlocked.Add(ref folderCount, subDirSize.FolderCount + 1);
                    _ = Interlocked.Add(ref fileCount, subDirSize.FileCount);
                    rootState?.AddFolder();
                }
                catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _ = errors.TryAdd(subDir, ex);
                }
            });

            var result = new FolderSize(folderPath, totalBytes, folderCount, fileCount, errors);
            if (_cache.TryAdd(folderPath, result))
            {
                PropertyChanged?.Invoke(this, new(nameof(CacheCount)));
            }
            return result;
        }
        finally
        {
            _ = semaphoreSlim.Release();
        }
    }

    /// <summary>
    /// 当前计算状态类, 用于报告进度
    /// </summary>
    /// <param name="_progress">进度报告</param>
    private sealed class State(IProgress<ProgressReport> _progress)
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
            _progress.Report(new(totalBytes, folderCount, fileCount));
        }

        /// <summary>
        /// 增加一个文件的大小到当前状态, 并报告进度
        /// </summary>
        /// <param name="bytes">文件大小</param>
        public void AddFile(long bytes)
        {
            var totalBytes = Interlocked.Add(ref _totalBytes, bytes);
            var fileCount = Interlocked.Increment(ref _fileCount);
            _progress.Report(new(totalBytes, _folderCount, fileCount));
        }

        /// <summary>
        /// 增加一个文件夹到当前状态, 并报告进度
        /// </summary>
        public void AddFolder()
        {
            var folderCount = Interlocked.Increment(ref _folderCount);
            _progress.Report(new(_totalBytes, folderCount, _fileCount));
        }
    }
}
