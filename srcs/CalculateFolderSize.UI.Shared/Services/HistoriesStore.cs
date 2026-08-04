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
/// <remarks>
/// 构造函数
/// </remarks>
/// <param name="_logger">日志记录器</param>
/// <param name="_coreOptions">Core 选项</param>
internal sealed partial class HistoriesStore(ILogger<HistoriesStore> _logger, CoreOptions _coreOptions) : IHistoriesStore
{
    /// <summary>
    /// 历史记录列表
    /// </summary>
    private readonly List<string> _histories = File.Exists(Constants.HistoriesFilePath)
        ? [.. File.ReadAllLines(Constants.HistoriesFilePath)] : [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Histories => _histories;

    /// <inheritdoc/>
    public async Task AddHistoriesAsync(IEnumerable<string> histories, CancellationToken cancellationToken = default)
    {
        foreach (var history in histories)
        {
            _ = _histories.RemoveAll(h => _coreOptions.PathComparer.Equals(h, history));
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
            removed += _histories.RemoveAll(h => _coreOptions.PathComparer.Equals(h, history));
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
            if (File.Exists(Constants.HistoriesFilePath))
            {
                File.Delete(Constants.HistoriesFilePath);
            }
        }
        catch (Exception ex)
        {
            LogClearHistoriesFailed(Constants.HistoriesFilePath, ex);
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
            await File.WriteAllLinesAsync(Constants.HistoriesFilePath, _histories, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            LogSaveHistoriesCanceled(Constants.HistoriesFilePath, ex);
        }
        catch (Exception ex)
        {
            LogSaveHistoriesFailed(Constants.HistoriesFilePath, ex);
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
