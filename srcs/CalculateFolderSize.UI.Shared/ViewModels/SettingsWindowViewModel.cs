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
/// 设置窗口视图模型, 负责 Core/UI 配置编辑、主题切换与关于信息
/// </summary>
public sealed partial class SettingsWindowViewModel : ToastViewModelBase
{
    /// <summary>
    /// 设置存储
    /// </summary>
    private readonly ISettingsStore _settingsStore;

    /// <summary>
    /// 主窗口提供器, 用于访问系统文件夹选择器
    /// </summary>
    private readonly IMainWindowProvider _mainWindowProvider;

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
    /// 进度节流间隔毫秒数
    /// </summary>
    [ObservableProperty]
    public partial decimal? ThrottleIntervalMilliseconds { get; set; }

    /// <summary>
    /// 日志级别
    /// </summary>
    [ObservableProperty]
    public partial LogLevel Level { get; set; }

    /// <summary>
    /// 可选择的日志级别列表
    /// </summary>
    public IReadOnlyList<LogLevel> Levels { get; } = Enum.GetValues<LogLevel>();

    /// <summary>
    /// 当前主题模式
    /// </summary>
    [ObservableProperty]
    public partial ThemeMode Theme { get; set; }

    /// <summary>
    /// 可选择的主题模式列表
    /// </summary>
    public IReadOnlyList<ThemeMode> Themes { get; } = Enum.GetValues<ThemeMode>();

    /// <summary>
    /// 产品名
    /// </summary>
    public string ProductName { get; }

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; }

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
    /// 日志按钮文本, 桌面端直接打开日志目录, 安卓端经 SAF 导出日志
    /// </summary>
    public string LogsButtonText { get; } =
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
        ? "打开日志文件夹" : "导出日志文件夹";

    /// <summary>
    /// 日志按钮的悬浮提示
    /// </summary>
    public string LogsFolderTip { get; } = Constants.LogsDirectory;

    /// <summary>
    /// 创建设置窗口视图模型
    /// </summary>
    public SettingsWindowViewModel(
        CoreOptions coreOptions,
        UIOptions uiOptions,
        ISettingsStore settingsStore,
        IMainWindowProvider mainWindowProvider)
    {
        _settingsStore = settingsStore;
        _mainWindowProvider = mainWindowProvider;

        DecimalPlaces = coreOptions.DecimalPlaces;
        MaxDegreeOfParallelism = coreOptions.MaxDegreeOfParallelism;
        CaptureChildren = coreOptions.CaptureChildren;
        ThrottleIntervalMilliseconds = uiOptions.ThrottleIntervalMilliseconds;
        Level = uiOptions.Level;
        Theme = uiOptions.Theme;

        var metadata = typeof(App).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToDictionary(a => a.Key, a => a.Value!);

        ProductName = metadata[nameof(ProductName)];
        Version = metadata[nameof(Version)];
        Author = metadata[nameof(Author)];
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

        await _settingsStore.UpdateUIOptionsAsync(o => o with
        {
            Level = Level,
            ThrottleIntervalMilliseconds = throttle
        });
        ExitApplication();
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
    /// 打开日志文件夹, 桌面端直接打开目录, 安卓端经 SAF 选择器导出日志到用户所选位置
    /// </summary>
    [RelayCommand]
    private async Task OpenLogsFolderAsync()
    {
        try
        {
            _ = Directory.CreateDirectory(Constants.LogsDirectory);

            if (OperatingSystem.IsAndroid())
            {
                await ExportLogsAsync(Constants.LogsDirectory);
                return;
            }
            if (!SystemOpener.TryOpen(Constants.LogsDirectory, _mainWindowProvider.MainWindow.Launcher, out var errorMessage))
            {
                ShowFeedback(errorMessage);
            }
        }
        catch (Exception ex)
        {
            ShowFeedback($"无法打开日志目录: {ex.Message}");
        }
    }

    /// <summary>
    /// 安卓端经 SAF 文件夹选择器将日志文件复制到用户所选目录
    /// </summary>
    /// <param name="logsDir">日志目录</param>
    private async Task ExportLogsAsync(string logsDir)
    {
        var topLevel = _mainWindowProvider.MainWindow;
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new()
        {
            Title = "选择日志导出位置"
        });
        if (folders.Count == 0)
        {
            return;
        }

        var logFiles = Directory.GetFiles(logsDir);
        if (logFiles.Length == 0)
        {
            ShowFeedback("日志目录为空, 没有可导出的日志");
            return;
        }

        try
        {
            foreach (var file in logFiles)
            {
                var fileName = Path.GetFileName(file);
                var targetFile = await folders[0].CreateFileAsync(fileName);
                if (targetFile is null)
                {
                    ShowFeedback($"日志导出失败: 无法在目标目录创建文件 {fileName}");
                    return;
                }
                await using var source = File.OpenRead(file);
                await using var destination = await targetFile.OpenWriteAsync();
                await source.CopyToAsync(destination);
            }
            ShowFeedback($"日志已导出到: {folders[0].Path}");
        }
        catch (Exception ex)
        {
            ShowFeedback($"日志导出失败: {ex.Message}");
        }
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
