using Avalonia.Input.Platform;
using Avalonia.Threading;
using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using CalculateFolderSize.UI.Shared.Interfaces;
using CalculateFolderSize.UI.Shared.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFolderSize.UI.Shared.ViewModels;

/// <summary>
/// 主窗口视图模型, 负责输入列表、历史记录、任务列表与缓存管理
/// </summary>
public sealed partial class MainWindowViewModel : ToastViewModelBase
{
    /// <summary>
    /// 文件系统
    /// </summary>
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// 文件夹大小计算器
    /// </summary>
    private readonly IFolderSizeCalculator _calculator;

    /// <summary>
    /// 历史存储
    /// </summary>
    private readonly IHistoriesStore _historiesStore;

    /// <summary>
    /// 存储访问服务, 用于检查全部文件访问权限并引导授权
    /// </summary>
    private readonly IStorageAccessService _storageAccessService;

    /// <summary>
    /// 主窗口提供器, 用于系统文件夹选择器与消息对话框
    /// </summary>
    private readonly IMainWindowProvider _mainWindowProvider;

    /// <summary>
    /// 路径比较使用的字符串比较器
    /// </summary>
    private readonly StringComparer _pathComparer;

    /// <summary>
    /// 缓存数刷新计时器, 定时拉取最新缓存数并合并更新, 避免每个目录完成时都向 UI 线程投递更新
    /// </summary>
    private readonly DispatcherTimer _cacheCountTimer;

    /// <summary>
    /// 当前是否可以开始计算
    /// </summary>
    public bool CanStart => InputPaths.Count > 0;

    /// <summary>
    /// 输入列表, 待计算的文件夹路径
    /// </summary>
    public ObservableCollection<string> InputPaths { get; } = [];

    /// <summary>
    /// 历史记录, 最近添加的路径在前
    /// </summary>
    public ObservableCollection<string> Histories { get; } = [];

    /// <summary>
    /// 任务列表
    /// </summary>
    public ObservableCollection<ScanTaskViewModel> Tasks { get; } = [];

    /// <summary>
    /// 是否显示存储访问权限横幅, 未授予全部文件访问权限时为 <see langword="true"/>
    /// </summary>
    [ObservableProperty]
    public partial bool ShowStorageAccessBanner { get; set; }

    /// <summary>
    /// 路径输入框内容
    /// </summary>
    [ObservableProperty]
    public partial string InputPath { get; set; } = string.Empty;

    /// <summary>
    /// 输入列表中选中的条目
    /// </summary>
    [ObservableProperty]
    public partial string? SelectedInputPath { get; set; }

    /// <summary>
    /// 当前缓存条目数
    /// </summary>
    [ObservableProperty]
    public partial int CacheCount { get; set; }

    /// <summary>
    /// 历史记录中选中的条目, 支持多选, 由视图在选中变化时更新
    /// </summary>
    public IReadOnlyList<string> SelectedHistories { get; set; } = [];

    /// <summary>
    /// 输入按钮的悬浮提示: 已选中条目时提示将覆盖该条目
    /// </summary>
    public string InputTip => SelectedInputPath is null
        ? "添加到待计算列表"
        : $"将覆盖已选择的输入: {SelectedInputPath}";

    /// <summary>
    /// 创建主窗口视图模型
    /// </summary>
    /// <param name="coreOptions">核心选项</param>
    /// <param name="fileSystem">文件系统</param>
    /// <param name="calculator">文件夹大小计算器</param>
    /// <param name="historiesStore">历史存储</param>
    /// <param name="storageAccessService">存储访问服务</param>
    /// <param name="mainWindowProvider">主窗口提供器</param>
    public MainWindowViewModel(
        CoreOptions coreOptions,
        IFileSystem fileSystem,
        IFolderSizeCalculator calculator,
        IHistoriesStore historiesStore,
        IStorageAccessService storageAccessService,
        IMainWindowProvider mainWindowProvider)
    {
        _fileSystem = fileSystem;
        _calculator = calculator;
        _historiesStore = historiesStore;
        _storageAccessService = storageAccessService;
        _mainWindowProvider = mainWindowProvider;
        _pathComparer = coreOptions.PathComparer;
        _cacheCountTimer = new(
            TimeSpan.FromMilliseconds(Constants.CacheCountRefreshIntervalMilliseconds),
            DispatcherPriority.Background,
            OnCacheCountTimerTick);
        _cacheCountTimer.Start();
        RefreshHistories();
        RefreshStorageAccess();
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        _cacheCountTimer.Stop();
        foreach (var task in Tasks)
        {
            task.DeleteRequested -= OnTaskDeleteRequested;
            task.ToastRequested -= ShowFeedback;
            task.Dispose();
        }
        base.Dispose();
    }

    /// <summary>
    /// 对任务列表按指定列排序
    /// </summary>
    /// <param name="key">排序列标识</param>
    /// <param name="ascending">是否升序</param>
    public void ApplySort(string key, bool ascending)
    {
        var comparer = key switch
        {
            nameof(ScanTaskViewModel.Path) => Comparer<ScanTaskViewModel>.Create((a, b) =>
                _pathComparer.Compare(a.Path, b.Path)),
            nameof(ScanTaskViewModel.StatusText) => Comparer<ScanTaskViewModel>.Create((a, b) =>
                {
                    var completed = a.IsCompleted.CompareTo(b.IsCompleted);
                    return completed != 0 ? completed : a.Status.CompareTo(b.Status);
                }),
            nameof(ScanTaskViewModel.StartTime) => Comparer<ScanTaskViewModel>.Create((a, b) =>
                a.StartTime.CompareTo(b.StartTime)),
            nameof(ScanTaskViewModel.Elapsed) => Comparer<ScanTaskViewModel>.Create((a, b) =>
                a.Elapsed.CompareTo(b.Elapsed)),
            nameof(ScanTaskViewModel.FoldersScanned) => Comparer<ScanTaskViewModel>.Create((a, b) =>
                a.FoldersScanned.CompareTo(b.FoldersScanned)),
            nameof(ScanTaskViewModel.FilesScanned) => Comparer<ScanTaskViewModel>.Create((a, b) =>
                a.FilesScanned.CompareTo(b.FilesScanned)),
            nameof(ScanTaskViewModel.SpeedBytesPerSecond) => Comparer<ScanTaskViewModel>.Create((a, b) =>
                a.SpeedBytesPerSecond.CompareTo(b.SpeedBytesPerSecond)),
            nameof(ScanTaskViewModel.BytesScanned) => Comparer<ScanTaskViewModel>.Create((a, b) =>
                a.BytesScanned.CompareTo(b.BytesScanned)),
            _ => Comparer<ScanTaskViewModel>.Default
        };

        var list = Tasks.ToList();
        list.Sort(comparer);
        if (!ascending)
        {
            list.Reverse();
        }

        Tasks.Clear();
        foreach (var task in list)
        {
            Tasks.Add(task);
        }
    }

    /// <summary>
    /// 选中输入列表条目时, 将路径载入输入框以便基于选中路径修改
    /// </summary>
    /// <param name="value">选中的路径</param>
    partial void OnSelectedInputPathChanged(string? value)
    {
        InputPath = value ?? string.Empty;
        OnPropertyChanged(nameof(InputTip));
    }

    /// <summary>
    /// 请求授予全部文件访问权限, 安卓端跳转系统设置页
    /// </summary>
    [RelayCommand]
    private void RequestAccess()
    {
        _storageAccessService.RequestAccess();
    }

    /// <summary>
    /// 一键清理缓存, 结果以短暂提示显示, 不弹窗
    /// </summary>
    [RelayCommand]
    private void ClearCache()
    {
        var cleared = _calculator.TryClearCache();
        ShowFeedback(cleared ? "缓存已清理" : "存在进行中的计算任务, 无法清理缓存");
    }

    /// <summary>
    /// 打开设置窗口
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        var viewModel = App.Services.GetRequiredService<SettingsWindowViewModel>();
        var window = new SettingsWindow { DataContext = viewModel };
        window.Show(_mainWindowProvider.MainWindow);
    }

    /// <summary>
    /// 弹出文件夹选择器并加入输入列表, 桌面端使用系统原生选择器, 安卓端使用内置目录浏览器
    /// </summary>
    [RelayCommand]
    private async Task PickFoldersAsync()
    {
        if (OperatingSystem.IsAndroid())
        {
            var viewModel = ActivatorUtilities.CreateInstance<DirectoryPickerViewModel>(App.Services);
            var window = new DirectoryPickerWindow { DataContext = viewModel };
            var selected = await window.ShowDialog<string?>(_mainWindowProvider.MainWindow);
            if (selected is not null && !InputPaths.Any(p => _pathComparer.Equals(p, selected)))
            {
                InputPaths.Add(selected);
                StartCalculationsCommand.NotifyCanExecuteChanged();
            }
            return;
        }

        var topLevel = _mainWindowProvider.MainWindow;
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new()
        {
            Title = "选择文件夹",
            AllowMultiple = true
        });

        var hasAdded = false;
        foreach (var folder in folders)
        {
            var uri = folder.Path;
            var path = uri.IsAbsoluteUri && uri.IsFile ? uri.LocalPath : uri.ToString();
            InputPaths.Add(path);
            hasAdded = true;
        }
        if (hasAdded)
        {
            StartCalculationsCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// 将路径输入框内容加入输入列表: 已存在则跳过, 已选中条目则覆盖, 否则添加
    /// </summary>
    [RelayCommand]
    private async Task AddInputAsync()
    {
        var path = InputPath.Trim();
        if (path.Length == 0 || InputPaths.Any(p => _pathComparer.Equals(p, path)))
        {
            return;
        }

        var selected = SelectedInputPath;
        if (selected is not null)
        {
            var index = InputPaths.IndexOf(selected);
            if (index >= 0)
            {
                InputPaths[index] = path;
                SelectedInputPath = null;
                InputPath = string.Empty;
                return;
            }
        }

        InputPath = string.Empty;
        InputPaths.Add(path);
        StartCalculationsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 将指定路径复制到剪贴板并显示短暂提示, 失败时同样给出可见反馈
    /// </summary>
    /// <param name="path">要复制的路径</param>
    [RelayCommand]
    private async Task CopyPathAsync(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var clipboard = _mainWindowProvider.MainWindow.Clipboard;
        if (clipboard is null)
        {
            ShowFeedback("剪贴板不可用");
            return;
        }

        try
        {
            await clipboard.SetTextAsync(path);
            ShowFeedback($"已复制: {path}");
        }
        catch (Exception)
        {
            ShowFeedback("复制失败");
        }
    }

    /// <summary>
    /// 删除输入列表中选中的条目
    /// </summary>
    [RelayCommand]
    private void RemoveSelectedInput()
    {
        if (SelectedInputPath is not null)
        {
            if (InputPaths.Remove(SelectedInputPath))
            {
                SelectedInputPath = null;
                StartCalculationsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 清空输入列表
    /// </summary>
    [RelayCommand]
    private void ClearInputs()
    {
        InputPaths.Clear();
        SelectedInputPath = null;
        StartCalculationsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 将历史记录中选中的条目加入输入列表
    /// </summary>
    [RelayCommand]
    private void AddFromHistory()
    {
        var hasAdded = false;
        foreach (var path in SelectedHistories)
        {
            if (!_fileSystem.DirectoryExists(path) || InputPaths.Any(p => _pathComparer.Equals(p, path)))
            {
                continue;
            }
            InputPaths.Add(path);
            hasAdded = true;
        }
        if (hasAdded)
        {
            StartCalculationsCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// 删除历史记录中选中的条目
    /// </summary>
    [RelayCommand]
    private async Task RemoveSelectedHistoriesAsync()
    {
        var selected = SelectedHistories;
        if (selected.Count == 0)
        {
            return;
        }

        await _historiesStore.RemoveHistoriesAsync(selected);

        SelectedHistories = [];
        RefreshHistories();
    }

    /// <summary>
    /// 清空历史记录
    /// </summary>
    [RelayCommand]
    private void ClearHistory()
    {
        _historiesStore.Clear();
        RefreshHistories();
    }

    /// <summary>
    /// 开始计算输入列表中的所有路径, 每个路径创建一个独立任务, 计算后写入历史记录
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartCalculationsAsync()
    {
        var paths = InputPaths.ToArray();
        foreach (var path in paths)
        {
            var task = ActivatorUtilities.CreateInstance<ScanTaskViewModel>(App.Services, path);
            task.DeleteRequested += OnTaskDeleteRequested;
            task.ToastRequested += ShowFeedback;
            Tasks.Add(task);
        }

        await _historiesStore.AddHistoriesAsync(paths);
        RefreshHistories();

        InputPaths.Clear();
        SelectedInputPath = null;
        StartCalculationsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 重新检查存储访问权限并更新横幅显示, 窗口从系统设置页授权返回时调用
    /// </summary>
    public void RefreshStorageAccess()
    {
        ShowStorageAccessBanner = !_storageAccessService.IsGranted;
    }

    /// <summary>
    /// 从历史存储刷新历史记录列表
    /// </summary>
    private void RefreshHistories()
    {
        Histories.Clear();
        foreach (var history in _historiesStore.Histories)
        {
            Histories.Add(history);
        }
    }

    /// <summary>
    /// 移除指定任务, 运行中的任务一并取消
    /// </summary>
    /// <param name="task">要移除的任务</param>
    private void OnTaskDeleteRequested(ScanTaskViewModel task)
    {
        task.DeleteRequested -= OnTaskDeleteRequested;
        task.ToastRequested -= ShowFeedback;
        _ = Tasks.Remove(task);
        task.Dispose();
    }

    /// <summary>
    /// 定时拉取缓存数: 与当前显示值不同才更新, 计时器常驻, 避免启停竞态导致显示停在旧值
    /// </summary>
    /// <param name="sender">计时器</param>
    /// <param name="e">计时器事件参数</param>
    private void OnCacheCountTimerTick(object? sender, EventArgs e)
    {
        var count = _calculator.CacheCount;
        if (CacheCount != count)
        {
            CacheCount = count;
        }
    }
}
