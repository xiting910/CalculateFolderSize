using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CalculateFolderSize.Core.Models;
using CalculateFolderSize.UI.Shared.ViewModels;

namespace CalculateFolderSize.UI.Shared.Views;

/// <summary>
/// 目录选择窗口, 安卓端用于浏览共享存储并选择文件夹
/// </summary>
public partial class DirectoryPickerWindow : Window
{
    /// <summary>
    /// 创建目录选择窗口
    /// </summary>
    public DirectoryPickerWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 双击子文件夹时进入
    /// </summary>
    /// <param name="sender">ListBox</param>
    /// <param name="e">双击事件参数</param>
    private void OnDirectoryDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Control { DataContext: DirectoryEntry directory } && DataContext is DirectoryPickerViewModel viewModel)
        {
            viewModel.Enter(directory);
        }
    }

    /// <summary>
    /// 确认选择当前目录并返回其路径
    /// </summary>
    /// <param name="sender">按钮</param>
    /// <param name="e">路由事件参数</param>
    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DirectoryPickerViewModel viewModel)
        {
            Close(viewModel.CurrentPath);
        }
    }

    /// <summary>
    /// 取消选择
    /// </summary>
    /// <param name="sender">按钮</param>
    /// <param name="e">路由事件参数</param>
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
