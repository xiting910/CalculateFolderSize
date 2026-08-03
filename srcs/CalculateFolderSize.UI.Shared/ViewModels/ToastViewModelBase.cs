using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFolderSize.UI.Shared.ViewModels;

/// <summary>
/// 带右下角 Toast 短暂提示能力的视图模型基类
/// </summary>
public abstract partial class ToastViewModelBase : ObservableObject, IDisposable
{
    /// <summary>
    /// 短暂提示文本, 以右下角 Toast 显示
    /// </summary>
    [ObservableProperty]
    public partial string Feedback { get; set; } = string.Empty;

    /// <summary>
    /// 短暂提示是否可见
    /// </summary>
    [ObservableProperty]
    public partial bool FeedbackVisible { get; set; }

    /// <summary>
    /// 用于延迟隐藏短暂提示的取消令牌源
    /// </summary>
    private CancellationTokenSource? _feedbackCts;

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        _feedbackCts?.Cancel();
        _feedbackCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 显示短暂提示并安排 3 秒后消失, 新提示会取代旧提示
    /// </summary>
    /// <param name="message">提示文本</param>
    public void ShowFeedback(string message)
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
            await Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
            if (ReferenceEquals(_feedbackCts, cts))
            {
                FeedbackVisible = false;
                Feedback = string.Empty;
            }
        }
        catch (OperationCanceledException) { }
    }
}
