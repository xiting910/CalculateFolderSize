using CalculateFolderSize.UI.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFolderSize.UI.Shared.ViewModels;

/// <summary>
/// 全局短暂提示视图模型, 供所有视图共用右下角 Toast 提示
/// </summary>
/// <param name="uiOptions">UI 层配置选项</param>
public sealed partial class ToastViewModel(UIOptions uiOptions) : ObservableObject, IDisposable
{
    /// <summary>
    /// 提示持续时间
    /// </summary>
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(uiOptions.ToastDurationSeconds);

    /// <summary>
    /// 用于延迟隐藏短暂提示的取消令牌源
    /// </summary>
    private CancellationTokenSource? _feedbackCts;

    /// <summary>
    /// 短暂提示文本
    /// </summary>
    [ObservableProperty]
    public partial string Feedback { get; set; } = string.Empty;

    /// <summary>
    /// 短暂提示是否可见
    /// </summary>
    [ObservableProperty]
    public partial bool FeedbackVisible { get; set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        _feedbackCts?.Cancel();
        _feedbackCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 显示短暂提示后消失, 新提示会取代旧提示
    /// </summary>
    /// <param name="message">提示文本</param>
    public void Show(string message)
    {
        _feedbackCts?.Cancel();
        _feedbackCts?.Dispose();
        _feedbackCts = new();
        Feedback = message;
        FeedbackVisible = true;
        _ = ClearFeedbackAfterAsync(_feedbackCts);
    }

    /// <summary>
    /// 延迟隐藏短暂提示
    /// </summary>
    /// <param name="cts">本次提示对应的取消令牌源</param>
    private async Task ClearFeedbackAfterAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(_delay, cts.Token);
            if (ReferenceEquals(_feedbackCts, cts))
            {
                FeedbackVisible = false;
                Feedback = string.Empty;
            }
        }
        catch (OperationCanceledException) { }
    }
}
