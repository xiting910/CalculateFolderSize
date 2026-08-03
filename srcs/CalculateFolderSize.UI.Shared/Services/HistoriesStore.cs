using CalculateFolderSize.Core.Models;
using CalculateFolderSize.UI.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFolderSize.UI.Shared.Services;

/// <summary>
/// 历史存储
/// </summary>
internal sealed partial class HistoriesStore : IHistoriesStore
{
    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<HistoriesStore> _logger;

    /// <summary>
    /// 要使用的路径比较器
    /// </summary>
    private readonly StringComparer _pathComparer;

    /// <summary>
    /// 历史记录文件路径
    /// </summary>
    private readonly string _historiesFilePath;

    /// <summary>
    /// 历史记录列表
    /// </summary>
    private readonly List<string> _histories;

    /// <inheritdoc/>
    public IReadOnlyList<string> Histories => _histories;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="coreOptions">核心选项</param>
    public HistoriesStore(ILogger<HistoriesStore> logger, CoreOptions coreOptions)
    {
        _logger = logger;
        _pathComparer = coreOptions.PathComparer;
        _historiesFilePath = Path.Combine(Constants.AppDataDirectory, Constants.HistoriesFileName);
        _histories = File.Exists(_historiesFilePath) ? [.. File.ReadAllLines(_historiesFilePath)] : [];
    }

    /// <inheritdoc/>
    public async Task AddHistoriesAsync(IEnumerable<string> histories, CancellationToken cancellationToken = default)
    {
        foreach (var history in histories)
        {
            _ = _histories.RemoveAll(h => _pathComparer.Equals(h, history));
            _histories.Insert(0, history);
        }

        await SaveHistoriesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveHistoriesAsync(IEnumerable<string> histories, CancellationToken cancellationToken = default)
    {
        var removed = 0;
        foreach (var history in histories)
        {
            removed += _histories.RemoveAll(h => _pathComparer.Equals(h, history));
        }
        if (removed > 0)
        {
            await SaveHistoriesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _histories.Clear();
        try
        {
            if (File.Exists(_historiesFilePath))
            {
                File.Delete(_historiesFilePath);
            }
        }
        catch (Exception ex)
        {
            LogClearHistoriesFailed(_historiesFilePath, ex);
        }
    }

    /// <summary>
    /// 异步保存历史记录到文件
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task SaveHistoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await File.WriteAllLinesAsync(_historiesFilePath, _histories, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            LogSaveHistoriesCanceled(_historiesFilePath, ex);
        }
        catch (Exception ex)
        {
            LogSaveHistoriesFailed(_historiesFilePath, ex);
        }
    }

    /// <summary>
    /// 记录保存历史记录被取消
    /// </summary>
    /// <param name="path">历史记录文件路径</param>
    /// <param name="exception">取消异常</param>
    [LoggerMessage(
        EventId = 1,
        EventName = "SaveHistoriesCanceled",
        Level = LogLevel.Information,
        Message = "保存历史记录被取消: {Path}"
    )]
    private partial void LogSaveHistoriesCanceled(string path, OperationCanceledException exception);

    /// <summary>
    /// 记录保存历史记录失败
    /// </summary>
    /// <param name="path">历史记录文件路径</param>
    /// <param name="exception">发生的异常</param>
    [LoggerMessage(
        EventId = 2,
        EventName = "SaveHistoriesFailed",
        Level = LogLevel.Warning,
        Message = "保存历史记录失败: {Path}"
    )]
    private partial void LogSaveHistoriesFailed(string path, Exception exception);

    /// <summary>
    /// 记录清空历史记录失败
    /// </summary>
    /// <param name="path">历史记录文件路径</param>
    /// <param name="exception">发生的异常</param>
    [LoggerMessage(
        EventId = 3,
        EventName = "ClearHistoriesFailed",
        Level = LogLevel.Warning,
        Message = "清空历史记录失败: {Path}"
    )]
    private partial void LogClearHistoriesFailed(string path, Exception exception);
}
