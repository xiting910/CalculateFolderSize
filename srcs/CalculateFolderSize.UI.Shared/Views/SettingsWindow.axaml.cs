using Avalonia.Controls;
using Avalonia.Input;
using CalculateFolderSize.UI.Shared.ViewModels;
using System;

namespace CalculateFolderSize.UI.Shared.Views;

/// <summary>
/// 设置窗口
/// </summary>
public partial class SettingsWindow : Window
{
    /// <summary>
    /// 创建设置窗口
    /// </summary>
    public SettingsWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 点击 GitHub 链接时用系统浏览器打开仓库
    /// </summary>
    /// <param name="sender">链接文本</param>
    /// <param name="e">指针事件参数</param>
    private async void OnGitHubLinkPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is SettingsWindowViewModel viewModel
            && !string.IsNullOrEmpty(viewModel.GitHubUrl)
            && GetTopLevel(this) is { } topLevel)
        {
            _ = await topLevel.Launcher.LaunchUriAsync(new Uri(viewModel.GitHubUrl));
        }
    }
}
