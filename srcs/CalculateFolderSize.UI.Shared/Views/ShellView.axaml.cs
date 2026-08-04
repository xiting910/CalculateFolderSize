using Avalonia.Controls;
using Avalonia.Input;
using CalculateFolderSize.UI.Shared.ViewModels;

namespace CalculateFolderSize.UI.Shared.Views;

/// <summary>
/// 壳视图, 承载主视图与各覆盖层, 是桌面与安卓共用的根视图
/// </summary>
public sealed partial class ShellView : UserControl
{
    /// <summary>
    /// 创建壳视图
    /// </summary>
    public ShellView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 点击设置抽屉遮罩时收起抽屉
    /// </summary>
    /// <param name="sender">遮罩</param>
    /// <param name="e">指针事件参数</param>
    private void OnSettingsMaskPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ShellViewModel viewModel)
        {
            viewModel.CloseSettings();
        }
    }
}
