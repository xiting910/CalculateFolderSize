using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CalculateFolderSize.UI.Shared.ViewModels;

namespace CalculateFolderSize.UI.Shared.Views;

/// <summary>
/// 设置视图
/// </summary>
public sealed partial class SettingsView : UserControl
{
    /// <summary>
    /// 创建设置视图
    /// </summary>
    public SettingsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 点击关闭按钮时请求收起设置抽屉
    /// </summary>
    /// <param name="sender">按钮</param>
    /// <param name="e">路由事件参数</param>
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.RequestClose();
        }
    }

    /// <summary>
    /// 点击 GitHub 链接时用系统浏览器打开仓库
    /// </summary>
    /// <param name="sender">链接文本</param>
    /// <param name="e">指针事件参数</param>
    private async void OnGitHubLinkPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel
            && !string.IsNullOrEmpty(viewModel.GitHubUrl)
            && TopLevel.GetTopLevel(this) is { } topLevel)
        {
            _ = await topLevel.Launcher.LaunchUriAsync(new(viewModel.GitHubUrl));
        }
    }
}
