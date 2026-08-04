using Avalonia.Input.Platform;
using Avalonia.Threading;
using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using CalculateFolderSize.UI.Shared.Interfaces;
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
/// 主视图模型, 负责输入列表、历史记录、任务列表与缓存管理
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// 状态列比较器, 已完成恒排在未完成之前, 相同则按状态排序
    /// </summary>
    private static readonly IComparer<CalculateTaskViewModel> StatusTextComparer =
        Comparer<CalculateTaskViewModel>.Create((a, b) =>
        {
            var completed = a.IsCompleted.CompareTo(b.IsCompleted);
            return completed != 0 ? completed : a.Status.CompareTo(b.Status);
        });

    /// <summary>
    /// 开始时间列比较器, 严格按开始时间排序
    /// </summary>
    private static readonly IComparer<CalculateTaskViewModel> StartTimeComparer =
        Comparer<CalculateTaskViewModel>.Create((a, b) => a.StartTime.CompareTo(b.StartTime));

    /// <summary>
    /// 耗时列比较器, 严格按耗时排序
    /// </summary>
    private static readonly IComparer<CalculateTaskViewModel> ElapsedComparer =
        Comparer<CalculateTaskViewModel>.Create((a, b) => a.Elapsed.CompareTo(b.Elapsed));

    /// <summary>
    /// 已计算文件夹数列比较器, 严格按文件夹数排序
    /// </summary>
    private static readonly IComparer<CalculateTaskViewModel> FoldersCalculatedComparer =
        Comparer<CalculateTaskViewModel>.Create((a, b) => a.FoldersCalculated.CompareTo(b.FoldersCalculated));

    /// <summary>
    /// 已计算文件数列比较器, 严格按文件数排序
    /// </summary>
    private static readonly IComparer<CalculateTaskViewModel> FilesCalculatedComparer =
        Comparer<CalculateTaskViewModel>.Create((a, b) => a.FilesCalculated.CompareTo(b.FilesCalculated));

    /// <summary>
    /// 速度列比较器, 严格按速度排序
    /// </summary>
    private static readonly IComparer<CalculateTaskViewModel> SpeedBytesPerSecondComparer =
        Comparer<CalculateTaskViewModel>.Create((a, b) =>
            a.SpeedBytesPerSecond.CompareTo(b.SpeedBytesPerSecond)
        );

    /// <summary>
    /// 已计算字节数列比较器, 严格按字节数排序
    /// </summary>
    private static readonly IComparer<CalculateTaskViewModel> BytesCalculatedComparer =
        Comparer<CalculateTaskViewModel>.Create((a, b) => a.BytesCalculated.CompareTo(b.BytesCalculated));

    /// <summary>
    /// 基于路径的计算任务视图模型比较器
    /// </summary>
    private readonly IComparer<CalculateTaskViewModel> PathComparer;

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
    /// 顶层视图提供器, 用于系统文件夹选择器与剪贴板
    /// </summary>
    private readonly ITopLevelProvider _topLevelProvider;

    /// <summary>
    /// 全局短暂提示视图模型
    /// </summary>
    private readonly ToastViewModel _toast;

    /// <summary>
    /// 路径比较使用的字符串比较器
    /// </summary>
    private readonly StringComparer _pathComparer;

    /// <summary>
    /// 界面状态刷新计时器, 定时拉取缓存条目数与存储访问权限并合并更新, 避免高频事件直接投递到 UI 线程
    /// </summary>
    private readonly DispatcherTimer _uiRefreshTimer;

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
    public ObservableCollection<CalculateTaskViewModel> Tasks { get; } = [];

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
    /// 输入按钮文本: 未选中条目时显示添加, 已选中时显示覆盖
    /// </summary>
    public string InputButtonText => SelectedInputPath is null ? "添加" : "覆盖";

    /// <summary>
    /// 当前是否可以开始计算
    /// </summary>
    public bool CanStart => InputPaths.Count > 0;

    /// <summary>
    /// 创建主视图模型
    /// </summary>
    /// <param name="coreOptions">Core 选项</param>
    /// <param name="fileSystem">文件系统</param>
    /// <param name="calculator">文件夹大小计算器</param>
    /// <param name="historiesStore">历史存储</param>
    /// <param name="storageAccessService">存储访问服务</param>
    /// <param name="topLevelProvider">顶层视图提供器</param>
    /// <param name="toastViewModel">全局短暂提示视图模型</param>
    public MainViewModel(
        CoreOptions coreOptions,
        IFileSystem fileSystem,
        IFolderSizeCalculator calculator,
        IHistoriesStore historiesStore,
        IStorageAccessService storageAccessService,
        ITopLevelProvider topLevelProvider,
        ToastViewModel toastViewModel)
    {
        _toast = toastViewModel;
        _fileSystem = fileSystem;
        _calculator = calculator;
        _historiesStore = historiesStore;
        _storageAccessService = storageAccessService;
        _topLevelProvider = topLevelProvider;
        _pathComparer = coreOptions.PathComparer;
        PathComparer = Comparer<CalculateTaskViewModel>.Create((a, b) =>
            _pathComparer.Compare(a.Path, b.Path)
        );
        _uiRefreshTimer = new(
            TimeSpan.FromMilliseconds(Constants.UiRefreshIntervalMilliseconds),
            DispatcherPriority.Background,
            OnUiRefreshTimerTick
        );
        _uiRefreshTimer.Start();
        RefreshStorageAccess();
        RefreshHistories();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _uiRefreshTimer.Stop();
        foreach (var task in Tasks)
        {
            task.DeleteRequested -= OnTaskDeleteRequested;
            task.ToastRequested -= _toast.Show;
            task.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 选中输入列表条目时, 将路径载入输入框以便基于选中路径修改
    /// </summary>
    /// <param name="value">选中的路径</param>
    partial void OnSelectedInputPathChanged(string? value)
    {
        InputPath = value ?? string.Empty;
        OnPropertyChanged(nameof(InputButtonText));
    }

    /// <summary>
    /// 请求打开设置抽屉的事件, 由壳视图模型处理
    /// </summary>
    public event Action? SettingsRequested;

    /// <summary>
    /// 请求打开目录选择器的事件, 由壳视图模型处理
    /// </summary>
    public event Action? DirectoryPickerRequested;

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
        _toast.Show(cleared ? "缓存已清理" : "存在进行中的计算任务, 无法清理缓存");
    }

    /// <summary>
    /// 打开设置抽屉
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        SettingsRequested?.Invoke();
    }

    /// <summary>
    /// 弹出文件夹选择器并加入输入列表, 桌面端使用系统原生选择器, 安卓端使用内置目录浏览器
    /// </summary>
    [RelayCommand]
    private async Task PickFoldersAsync()
    {
        if (OperatingSystem.IsAndroid())
        {
            DirectoryPickerRequested?.Invoke();
            return;
        }

        var topLevel = _topLevelProvider.TopLevel;
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

        var clipboard = _topLevelProvider.TopLevel.Clipboard;
        if (clipboard is null)
        {
            _toast.Show("剪贴板不可用");
            return;
        }

        try
        {
            await clipboard.SetTextAsync(path);
            _toast.Show($"已复制: {path}");
        }
        catch (Exception)
        {
            _toast.Show("复制失败");
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
            var task = ActivatorUtilities.CreateInstance<CalculateTaskViewModel>(App.Services, path);
            task.DeleteRequested += OnTaskDeleteRequested;
            task.ToastRequested += _toast.Show;
            Tasks.Add(task);
        }

        await _historiesStore.AddHistoriesAsync(paths);
        RefreshHistories();

        InputPaths.Clear();
        SelectedInputPath = null;
        StartCalculationsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 重新检查存储访问权限并更新横幅显示, 由缓存数计时器定时调用, 权限变化时自动刷新
    /// </summary>
    public void RefreshStorageAccess()
    {
        var showBanner = !_storageAccessService.IsGranted;
        if (ShowStorageAccessBanner != showBanner)
        {
            ShowStorageAccessBanner = showBanner;
        }
    }

    /// <summary>
    /// 显示全局短暂提示, 供视图直接调用
    /// </summary>
    /// <param name="message">提示文本</param>
    public void ShowToast(string message)
    {
        _toast.Show(message);
    }

    /// <summary>
    /// 将指定路径加入输入列表, 已存在则跳过
    /// </summary>
    /// <param name="path">文件夹路径</param>
    public void AddInputPath(string path)
    {
        if (!InputPaths.Any(p => _pathComparer.Equals(p, path)))
        {
            InputPaths.Add(path);
            StartCalculationsCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// 对任务列表按指定列排序
    /// </summary>
    /// <param name="key">排序列标识</param>
    /// <param name="ascending">是否升序</param>
    public void ApplySort(string key, bool ascending)
    {
        var list = Tasks.ToList();
        list.Sort(key switch
        {
            nameof(CalculateTaskViewModel.Path) => PathComparer,
            nameof(CalculateTaskViewModel.StatusText) => StatusTextComparer,
            nameof(CalculateTaskViewModel.StartTime) => StartTimeComparer,
            nameof(CalculateTaskViewModel.Elapsed) => ElapsedComparer,
            nameof(CalculateTaskViewModel.FoldersCalculated) => FoldersCalculatedComparer,
            nameof(CalculateTaskViewModel.FilesCalculated) => FilesCalculatedComparer,
            nameof(CalculateTaskViewModel.SpeedBytesPerSecond) => SpeedBytesPerSecondComparer,
            nameof(CalculateTaskViewModel.BytesCalculated) => BytesCalculatedComparer,
            _ => Comparer<CalculateTaskViewModel>.Default
        });
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
    private void OnTaskDeleteRequested(CalculateTaskViewModel task)
    {
        task.DeleteRequested -= OnTaskDeleteRequested;
        task.ToastRequested -= _toast.Show;
        _ = Tasks.Remove(task);
        task.Dispose();
    }

    /// <summary>
    /// 定时刷新界面状态: 拉取缓存条目数与存储访问权限, 与当前显示值不同才更新, 计时器常驻
    /// </summary>
    /// <param name="sender">计时器</param>
    /// <param name="e">计时器事件参数</param>
    private void OnUiRefreshTimerTick(object? sender, EventArgs e)
    {
        var count = _calculator.CacheCount;
        if (CacheCount != count)
        {
            CacheCount = count;
        }
        RefreshStorageAccess();
    }
}
