using Avalonia.Media;
using Avalonia.Threading;
using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using CalculateFolderSize.UI.Shared.Interfaces;
using CalculateFolderSize.UI.Shared.Models;
using CalculateFolderSize.UI.Shared.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFolderSize.UI.Shared.ViewModels;

/// <summary>
/// 单个扫描任务视图模型, 负责任务执行、进度统计与状态流转
/// </summary>
public sealed partial class ScanTaskViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// 已打开的结果窗口, 按路径去重
    /// </summary>
    private readonly Dictionary<string, ResultWindow> ResultWindows;

    /// <summary>
    /// 文件大小格式化器
    /// </summary>
    private readonly IFileSizeFormatter _formatter;

    /// <summary>
    /// 文件夹大小计算器
    /// </summary>
    private readonly IFolderSizeCalculator _calculator;

    /// <summary>
    /// 主窗口提供器, 用于消息对话框与窗口所有者
    /// </summary>
    private readonly IMainWindowProvider _mainWindowProvider;

    /// <summary>
    /// 取消令牌源
    /// </summary>
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// 耗时刷新计时器, 独立于进度更新, 保证无进度上报时耗时列仍会刷新
    /// </summary>
    private readonly DispatcherTimer _elapsedTimer;

    /// <summary>
    /// 时间提供器, 用于计算耗时
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// 是否开启子项缓存, 影响是否可以打开结果窗口
    /// </summary>
    private readonly bool _captureChildren;

    /// <summary>
    /// 上次看到的缓存条目数, 缓存被清理时数值下降, 此时需要重查结果可用性
    /// </summary>
    private long _lastSeenCacheCount;

    /// <summary>
    /// 要计算的文件夹路径
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 任务状态
    /// </summary>
    [ObservableProperty]
    public partial CalculateTaskStatus Status { get; set; }

    /// <summary>
    /// 任务开始时间
    /// </summary>
    [ObservableProperty]
    public partial DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// 已扫描的字节数
    /// </summary>
    [ObservableProperty]
    public partial long BytesScanned { get; set; }

    /// <summary>
    /// 已扫描的文件夹数
    /// </summary>
    [ObservableProperty]
    public partial int FoldersScanned { get; set; }

    /// <summary>
    /// 已扫描的文件数
    /// </summary>
    [ObservableProperty]
    public partial int FilesScanned { get; set; }

    /// <summary>
    /// 当前扫描速度
    /// </summary>
    [ObservableProperty]
    public partial double SpeedBytesPerSecond { get; set; }

    /// <summary>
    /// 已耗时
    /// </summary>
    [ObservableProperty]
    public partial TimeSpan Elapsed { get; set; }

    /// <summary>
    /// 是否可以打开结果窗口
    /// </summary>
    [ObservableProperty]
    public partial bool CanOpenResult { get; set; }

    /// <summary>
    /// 扫描结果, 完成后可用
    /// </summary>
    [ObservableProperty]
    public partial FolderSize? Result { get; set; }

    /// <summary>
    /// 创建扫描任务视图模型
    /// </summary>
    public ScanTaskViewModel(
        CoreOptions coreOptions,
        IFileSizeFormatter formatter,
        IFolderSizeCalculator calculator,
        ICalculateProgress progress,
        IMainWindowProvider mainWindowProvider,
        TimeProvider timeProvider,
        string path)
    {
        ResultWindows = new(coreOptions.PathComparer);
        _formatter = formatter;
        _calculator = calculator;
        _calculator.PropertyChanged += OnCalculatorPropertyChanged;
        _lastSeenCacheCount = _calculator.CacheCount;
        _mainWindowProvider = mainWindowProvider;
        _elapsedTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(Constants.ElapsedUpdateIntervalMilliseconds)
        };
        _timeProvider = timeProvider;
        _captureChildren = coreOptions.CaptureChildren;
        Path = path;
        Status = CalculateTaskStatus.Running;
        StartTime = _timeProvider.GetUtcNow();
        progress.ProgressUpdated += OnProgressUpdated;
        _elapsedTimer.Tick += OnElapsedTimerTick;
        _ = RunAsync(progress, _cts.Token);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _elapsedTimer.Stop();
        _elapsedTimer.Tick -= OnElapsedTimerTick;
        _calculator.PropertyChanged -= OnCalculatorPropertyChanged;
        _cts.Cancel();
        _cts.Dispose();
    }

    /// <summary>
    /// 任务状态文本
    /// </summary>
    public string StatusText => Status.GetDescription();

    /// <summary>
    /// 开始时间的本地时间文本, 内部以 UTC 存储, 展示时转为本地时区
    /// </summary>
    public string StartTimeText => StartTime.ToLocalTime().ToString("HH:mm:ss");

    /// <summary>
    /// 删除按钮的动态文本: 运行中显示取消, 其他状态显示删除, 点击行为一致
    /// </summary>
    public string DeleteButtonText => Status is CalculateTaskStatus.Running ? "取消" : "删除";

    /// <summary>
    /// 任务状态对应的前景色画刷, 用于状态文本着色区分
    /// </summary>
    public IBrush? StatusBrush => Status switch
    {
        CalculateTaskStatus.Running => Brushes.SteelBlue,
        CalculateTaskStatus.Completed => Brushes.SeaGreen,
        CalculateTaskStatus.Cancelled => Brushes.Gray,
        CalculateTaskStatus.DirectoryNotFound => Brushes.DarkOrange,
        CalculateTaskStatus.Failed => Brushes.IndianRed,
        _ => null
    };

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted => Status is CalculateTaskStatus.Completed;

    /// <summary>
    /// 已扫描字节数的格式化文本
    /// </summary>
    public string BytesText => _formatter.Format(BytesScanned);

    /// <summary>
    /// 扫描速度的格式化文本
    /// </summary>
    public string SpeedText => _formatter.Format((long)Math.Round(SpeedBytesPerSecond)) + "/s";

    /// <summary>
    /// 耗时的格式化文本
    /// </summary>
    public string ElapsedText => $"{(int)Elapsed.TotalHours:00}:{Elapsed.Minutes:00}:{Elapsed.Seconds:00}";

    /// <summary>
    /// 无法查看结果时的提示文本, 按任务状态与失败原因区分
    /// </summary>
    public string OpenResultHint => Status is CalculateTaskStatus.Completed
        ? _captureChildren ? "缓存已清理, 无法查看结果" : "未开启子项捕获, 无法查看结果"
        : Status switch
        {
            CalculateTaskStatus.Running => "任务正在运行中, 请在完成后双击查看结果",
            CalculateTaskStatus.DirectoryNotFound => "目录不存在, 无法查看结果",
            CalculateTaskStatus.Cancelled => "任务已取消, 无法查看结果",
            CalculateTaskStatus.Failed => "任务失败, 无法查看结果",
            _ => $"未知的状态: {Status}, 无法查看结果"
        };

    /// <summary>
    /// 开始时间变化时刷新本地时间文本
    /// </summary>
    /// <param name="value">新值</param>
    partial void OnStartTimeChanged(DateTimeOffset value)
    {
        OnPropertyChanged(nameof(StartTimeText));
    }

    /// <summary>
    /// 状态变化时的联动
    /// </summary>
    /// <param name="value">新状态</param>
    partial void OnStatusChanged(CalculateTaskStatus value)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(StatusBrush));
        OnPropertyChanged(nameof(DeleteButtonText));
        RecheckCanOpenResult();
    }

    /// <summary>
    /// 已扫描字节数变化时刷新格式化文本
    /// </summary>
    /// <param name="value">新值</param>
    partial void OnBytesScannedChanged(long value)
    {
        OnPropertyChanged(nameof(BytesText));
    }

    /// <summary>
    /// 扫描速度变化时刷新格式化文本
    /// </summary>
    /// <param name="value">新值</param>
    partial void OnSpeedBytesPerSecondChanged(double value)
    {
        OnPropertyChanged(nameof(SpeedText));
    }

    /// <summary>
    /// 耗时变化时刷新格式化文本
    /// </summary>
    /// <param name="value">新值</param>
    partial void OnElapsedChanged(TimeSpan value)
    {
        OnPropertyChanged(nameof(ElapsedText));
    }

    /// <summary>
    /// 请求删除当前任务的事件, 由持有该任务的视图模型处理
    /// </summary>
    public event Action<ScanTaskViewModel>? DeleteRequested;

    /// <summary>
    /// 请求显示短暂提示的事件, 由持有该任务的视图模型处理
    /// </summary>
    public event Action<string>? ToastRequested;

    /// <summary>
    /// 取消当前任务
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _cts.Cancel();
    }

    /// <summary>
    /// 请求从任务列表中删除当前任务
    /// </summary>
    [RelayCommand]
    private void Delete()
    {
        DeleteRequested?.Invoke(this);
    }

    /// <summary>
    /// 打开结果窗口
    /// </summary>
    public void OpenResult()
    {
        if (Result is null) { return; }
        if (ResultWindows.TryGetValue(Path, out var existing))
        {
            existing.Activate();
            return;
        }

        if (_calculator.TryGetFolderChildren(Path, out _))
        {
            var window = new ResultWindow
            {
                DataContext = ActivatorUtilities.CreateInstance<ResultWindowViewModel>(App.Services, Path, Result)
            };
            window.Closed += (_, _) => _ = ResultWindows.Remove(Path);
            ResultWindows[Path] = window;
            window.Show(_mainWindowProvider.MainWindow);
        }
        else
        {
            ToastRequested?.Invoke("该文件夹的子项缓存已失效, 请重新扫描后重试");
        }
    }

    /// <summary>
    /// 在后台执行计算并处理状态流转
    /// </summary>
    /// <param name="progress">计算进度</param>
    /// <param name="token">取消令牌</param>
    private async Task RunAsync(ICalculateProgress progress, CancellationToken token)
    {
        _elapsedTimer.Start();
        try
        {
            var folderSize = await Task.Run(() => _calculator.GetFromFolder(Path, progress, token), token);
            progress.Complete();
            if (folderSize is null)
            {
                Status = CalculateTaskStatus.DirectoryNotFound;
            }
            else
            {
                Result = folderSize;
                BytesScanned = folderSize.TotalBytes;
                FilesScanned = folderSize.FileCount;
                Status = CalculateTaskStatus.Completed;
            }
        }
        catch (OperationCanceledException)
        {
            Status = CalculateTaskStatus.Cancelled;
        }
        catch (Exception)
        {
            Status = CalculateTaskStatus.Failed;
        }
        finally
        {
            _elapsedTimer.Stop();
            Elapsed = _timeProvider.GetUtcNow() - StartTime;
            progress.ProgressUpdated -= OnProgressUpdated;
        }
    }

    /// <summary>
    /// 重新检查是否可以打开结果窗口
    /// </summary>
    private void RecheckCanOpenResult()
    {
        CanOpenResult = Status is CalculateTaskStatus.Completed && _captureChildren
            && _calculator.TryGetFolderChildren(Path, out _);
    }

    /// <summary>
    /// 计时器刷新耗时, 与进度更新无关
    /// </summary>
    /// <param name="sender">计时器</param>
    /// <param name="e">事件参数</param>
    private void OnElapsedTimerTick(object? sender, EventArgs e)
    {
        Elapsed = _timeProvider.GetUtcNow() - StartTime;
    }

    /// <summary>
    /// 进度更新回调
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">进度更新事件参数</param>
    private void OnProgressUpdated(object? sender, CalculateProgressUpdateEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            BytesScanned = e.ProgressReport.BytesSoFar;
            FilesScanned = e.ProgressReport.FilesProcessed;
            FoldersScanned = e.ProgressReport.FoldersProcessed;
            SpeedBytesPerSecond = e.SpeedBytesPerSecond;
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// 缓存数变化时检查是否需要重查结果可用性
    /// </summary>
    /// <param name="sender">计算器</param>
    /// <param name="e">属性变化事件</param>
    private void OnCalculatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IFolderSizeCalculator.CacheCount))
        {
            return;
        }

        var count = _calculator.CacheCount;
        if (count >= _lastSeenCacheCount)
        {
            _lastSeenCacheCount = count;
            return;
        }

        _lastSeenCacheCount = count;
        Dispatcher.UIThread.Post(RecheckCanOpenResult, DispatcherPriority.Background);
    }
}
