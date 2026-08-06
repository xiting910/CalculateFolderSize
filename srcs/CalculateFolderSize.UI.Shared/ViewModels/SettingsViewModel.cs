using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using CalculateFolderSize.Core.Models;
using CalculateFolderSize.UI.Shared.Interfaces;
using CalculateFolderSize.UI.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CalculateFolderSize.UI.Shared.ViewModels;

/// <summary>
/// 设置视图模型, 负责 Core/UI 配置编辑、主题切换与关于信息
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    /// <summary>
    /// 设置存储
    /// </summary>
    private readonly ISettingsStore _settingsStore;

    /// <summary>
    /// 全局短暂提示视图模型
    /// </summary>
    private readonly ToastViewModel _toast;

    /// <summary>
    /// 小数位数
    /// </summary>
    [ObservableProperty]
    public partial decimal? DecimalPlaces { get; set; }

    /// <summary>
    /// 最大并行度
    /// </summary>
    [ObservableProperty]
    public partial decimal? MaxDegreeOfParallelism { get; set; }

    /// <summary>
    /// 是否捕获子项
    /// </summary>
    [ObservableProperty]
    public partial bool CaptureChildren { get; set; }

    /// <summary>
    /// 当前主题模式
    /// </summary>
    [ObservableProperty]
    public partial ThemeMode Theme { get; set; }

    /// <summary>
    /// 进度节流间隔毫秒数
    /// </summary>
    [ObservableProperty]
    public partial decimal? ThrottleIntervalMilliseconds { get; set; }

    /// <summary>
    /// Toast 提示显示时间秒数
    /// </summary>
    [ObservableProperty]
    public partial decimal? ToastDurationSeconds { get; set; }

    /// <summary>
    /// 日志级别
    /// </summary>
    [ObservableProperty]
    public partial LogLevel Level { get; set; }

    /// <summary>
    /// 可选择的主题模式列表
    /// </summary>
    public IReadOnlyList<ThemeMode> Themes { get; } = Enum.GetValues<ThemeMode>();

    /// <summary>
    /// 可选择的日志级别列表
    /// </summary>
    public IReadOnlyList<LogLevel> Levels { get; } = Enum.GetValues<LogLevel>();

    /// <summary>
    /// 产品
    /// </summary>
    public string Product { get; }

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// 作者
    /// </summary>
    public string Authors { get; }

    /// <summary>
    /// 许可证
    /// </summary>
    public string License { get; }

    /// <summary>
    /// GitHub 仓库地址
    /// </summary>
    public string GitHubUrl { get; }

    /// <summary>
    /// 保存按钮文本, 桌面端保存后退出整个应用, 其他平台仅保存
    /// </summary>
    public string SaveButtonText { get; } =
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
        ? "保存并退出" : "保存";

    /// <summary>
    /// 日志按钮文本, 桌面端打开日志目录, 安卓端提示日志路径 (安卓无打开文件夹的系统能力)
    /// </summary>
    public string LogsButtonText { get; } = OperatingSystem.IsAndroid() ? "日志目录" : "打开日志文件夹";

    /// <summary>
    /// 日志按钮的悬浮提示
    /// </summary>
    public string LogsFolderTip { get; } = Constants.LogsDirectory;

    /// <summary>
    /// 创建设置视图模型
    /// </summary>
    /// <param name="coreOptions">Core 配置</param>
    /// <param name="uiOptions">UI 配置</param>
    /// <param name="settingsStore">设置存储</param>
    /// <param name="toastViewModel">全局短暂提示视图模型</param>
    public SettingsViewModel(
        CoreOptions coreOptions,
        UIOptions uiOptions,
        ISettingsStore settingsStore,
        ToastViewModel toastViewModel)
    {
        _settingsStore = settingsStore;
        _toast = toastViewModel;

        DecimalPlaces = coreOptions.DecimalPlaces;
        MaxDegreeOfParallelism = coreOptions.MaxDegreeOfParallelism;
        CaptureChildren = coreOptions.CaptureChildren;
        Theme = uiOptions.Theme;
        ThrottleIntervalMilliseconds = uiOptions.ThrottleIntervalMilliseconds;
        ToastDurationSeconds = (decimal?)uiOptions.ToastDurationSeconds;
        Level = uiOptions.Level;

        var metadata = typeof(App).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToDictionary(a => a.Key, a => a.Value!);

        Product = metadata[nameof(Product)];
        Version = metadata[nameof(Version)];
        Authors = metadata[nameof(Authors)];
        License = metadata[nameof(License)];
        GitHubUrl = metadata[nameof(GitHubUrl)];
    }

    /// <summary>
    /// 主题变化时切换主题
    /// </summary>
    /// <param name="value">新主题模式</param>
    partial void OnThemeChanged(ThemeMode value)
    {
        _ = SetThemeAsync(value);
    }

    /// <summary>
    /// 保存 Core 计算设置
    /// </summary>
    [RelayCommand]
    private async Task SaveCoreOptionsAsync()
    {
        var decimalPlaces = Math.Clamp(
            (int)(DecimalPlaces ?? 2),
            Constants.MinDecimalPlaces,
            Constants.MaxDecimalPlaces
        );
        var maxDegree = Math.Clamp(
            (int)(MaxDegreeOfParallelism ?? (Environment.ProcessorCount * 2)),
            Constants.MinParallelism,
            Constants.MaxParallelism
        );

        await _settingsStore.UpdateCoreOptionsAsync(o => o with
        {
            DecimalPlaces = decimalPlaces,
            MaxDegreeOfParallelism = maxDegree,
            CaptureChildren = CaptureChildren
        });
        ExitApplication();
    }

    /// <summary>
    /// 保存 UI 配置
    /// </summary>
    [RelayCommand]
    private async Task SaveUIOptionsAsync()
    {
        var throttle = (int)(ThrottleIntervalMilliseconds ?? 100);
        if (throttle < Constants.MinThrottleIntervalMilliseconds)
        {
            throttle = Constants.MinThrottleIntervalMilliseconds;
        }
        else if (throttle > Constants.MaxThrottleIntervalMilliseconds)
        {
            throttle = Constants.MaxThrottleIntervalMilliseconds;
        }

        var toastDuration = (double)(ToastDurationSeconds ?? 3);
        if (toastDuration < Constants.MinToastDurationSeconds)
        {
            toastDuration = Constants.MinToastDurationSeconds;
        }
        else if (toastDuration > Constants.MaxToastDurationSeconds)
        {
            toastDuration = Constants.MaxToastDurationSeconds;
        }

        await _settingsStore.UpdateUIOptionsAsync(o => o with
        {
            ThrottleIntervalMilliseconds = throttle,
            ToastDurationSeconds = toastDuration,
            Level = Level
        });
        ExitApplication();
    }

    /// <summary>
    /// 打开日志文件夹, 桌面端直接打开目录, 安卓端提示日志路径 (日志位于共享存储, 可直接用文件管理器浏览)
    /// </summary>
    [RelayCommand]
    private void OpenLogsFolder()
    {
        try
        {
            _ = Directory.CreateDirectory(Constants.LogsDirectory);

            if (OperatingSystem.IsAndroid())
            {
                _toast.Show($"日志目录: {Constants.LogsDirectory}");
                return;
            }

            if (!SystemOpener.TryOpen(Constants.LogsDirectory, out var errorMessage))
            {
                _toast.Show(errorMessage);
            }
        }
        catch (Exception ex)
        {
            _toast.Show($"无法打开日志目录: {ex.Message}");
        }
    }

    /// <summary>
    /// 请求关闭设置抽屉的事件, 由壳视图模型处理
    /// </summary>
    public event Action? CloseRequested;

    /// <summary>
    /// 请求关闭设置抽屉
    /// </summary>
    public void RequestClose()
    {
        CloseRequested?.Invoke();
    }

    /// <summary>
    /// 切换主题, 立即生效并持久化
    /// </summary>
    /// <param name="theme">目标主题模式</param>
    private async Task SetThemeAsync(ThemeMode theme)
    {
        Application.Current?.RequestedThemeVariant = theme switch
        {
            ThemeMode.System => ThemeVariant.Default,
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            _ => throw new ArgumentOutOfRangeException(nameof(theme), "未知的主题模式")
        };
        await _settingsStore.UpdateUIOptionsAsync(o => o with { Theme = theme });
    }

    /// <summary>
    /// 退出整个应用, 用于保存设置后使改动在下次启动时生效
    /// </summary>
    private static void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
