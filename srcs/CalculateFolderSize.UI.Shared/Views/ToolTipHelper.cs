using Avalonia.Controls;
using Avalonia.Input;

namespace CalculateFolderSize.UI.Shared.Views;

/// <summary>
/// 悬浮提示辅助类, 支持触摸长按打开
/// </summary>
public static class ToolTipHelper
{
    /// <summary>
    /// 触摸长按控件时打开或关闭其悬浮提示, 桌面端仍由鼠标悬浮触发
    /// </summary>
    /// <param name="sender">提示控件</param>
    /// <param name="e">长按事件参数</param>
    public static void ToggleByHolding(object? sender, HoldingRoutedEventArgs e)
    {
        if (sender is Control control)
        {
            ToolTip.SetIsOpen(control, e.HoldingState == HoldingState.Started);
        }
    }
}
