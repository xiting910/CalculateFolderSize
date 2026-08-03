using Avalonia.Controls;

namespace CalculateFolderSize.UI.Shared.Views;

/// <summary>
/// 右下角短暂提示视图, 绑定视图模型的 Toast 反馈属性
/// </summary>
public partial class ToastView : UserControl
{
    /// <summary>
    /// 创建短暂提示视图
    /// </summary>
    public ToastView()
    {
        InitializeComponent();
    }
}
