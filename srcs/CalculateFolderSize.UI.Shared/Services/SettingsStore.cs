using CalculateFolderSize.Core;
using CalculateFolderSize.Core.Models;
using CalculateFolderSize.UI.Shared.Interfaces;
using CalculateFolderSize.UI.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFolderSize.UI.Shared.Services;

/// <summary>
/// 设置存储
/// </summary>
/// <param name="_logger">日志记录器</param>
/// <param name="_coreOptions">核心配置选项</param>
/// <param name="_uiOptions">UI 层配置选项</param>
internal sealed partial class SettingsStore(
    ILogger<SettingsStore> _logger,
    CoreOptions _coreOptions,
    UIOptions _uiOptions
) : ISettingsStore
{
    /// <summary>
    /// 设置文件路径
    /// </summary>
    private readonly string _settingsFilePath = Path.Combine(
        Constants.AppDataDirectory,
        Constants.SettingsFileName
    );

    /// <inheritdoc/>
    public async Task UpdateCoreOptionsAsync(Func<CoreOptions, CoreOptions> updateFunc, CancellationToken token = default)
    {
        _coreOptions = updateFunc(_coreOptions);
        await SaveSettingsAsync(token);
    }

    /// <inheritdoc/>
    public async Task UpdateUIOptionsAsync(Func<UIOptions, UIOptions> updateFunc, CancellationToken token = default)
    {
        _uiOptions = updateFunc(_uiOptions);
        await SaveSettingsAsync(token);
    }

    /// <summary>
    /// 异步保存设置到文件
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task SaveSettingsAsync(CancellationToken cancellationToken = default)
    {
        var json = new JsonObject
        {
            [nameof(Core)] = new JsonObject
            {
                [nameof(CoreOptions.DecimalPlaces)] = _coreOptions.DecimalPlaces,
                [nameof(CoreOptions.MaxDegreeOfParallelism)] = _coreOptions.MaxDegreeOfParallelism,
                [nameof(CoreOptions.CaptureChildren)] = _coreOptions.CaptureChildren,
                [nameof(CoreOptions.PathComparer)] = _coreOptions.PathComparer.ToJsonString()
            },
            [nameof(UI)] = new JsonObject
            {
                [nameof(UIOptions.Level)] = _uiOptions.Level.ToString(),
                [nameof(UIOptions.Theme)] = _uiOptions.Theme.ToString(),
                [nameof(UIOptions.ThrottleIntervalMilliseconds)] = _uiOptions.ThrottleIntervalMilliseconds
            }
        };
        var jsonString = json.ToJsonString(Constants.JsonSerializerOptions);

        try
        {
            await File.WriteAllTextAsync(_settingsFilePath, jsonString, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            LogSaveSettingsCanceled(_settingsFilePath, ex);
        }
        catch (Exception ex)
        {
            LogSaveSettingsFailed(_settingsFilePath, ex);
        }
    }

    /// <summary>
    /// 记录保存设置被取消
    /// </summary>
    /// <param name="path">设置文件路径</param>
    /// <param name="exception">取消异常</param>
    [LoggerMessage(
        EventId = 1,
        EventName = "SaveSettingsCanceled",
        Level = LogLevel.Information,
        Message = "保存设置被取消: {Path}"
    )]
    private partial void LogSaveSettingsCanceled(string path, OperationCanceledException exception);

    /// <summary>
    /// 记录保存设置失败
    /// </summary>
    /// <param name="path">设置文件路径</param>
    /// <param name="exception">发生的异常</param>
    [LoggerMessage(
        EventId = 2,
        EventName = "SaveSettingsFailed",
        Level = LogLevel.Warning,
        Message = "保存设置失败: {Path}"
    )]
    private partial void LogSaveSettingsFailed(string path, Exception exception);
}
