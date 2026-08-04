using Avalonia.Controls;

namespace CalculateFolderSize.UI.Shared.Views;

/// <summary>
/// 桌面端壳窗口, 仅承载 <see cref="ShellView"/>, 安卓端不使用
/// </summary>
public sealed partial class ShellWindow : Window
{
    /// <summary>
    /// 创建桌面端壳窗口
    /// </summary>
    public ShellWindow()
    {
        InitializeComponent();
    }
}
