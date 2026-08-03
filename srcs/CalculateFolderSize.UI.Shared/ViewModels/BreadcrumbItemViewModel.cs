using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace CalculateFolderSize.UI.Shared.ViewModels;

/// <summary>
/// 面包屑条目视图模型
/// </summary>
/// <param name="name">显示名称</param>
/// <param name="navigate">点击时的导航回调</param>
public sealed partial class BreadcrumbItemViewModel(string name, Action navigate) : ObservableObject
{
    /// <summary>
    /// 显示名称
    /// </summary>
    [ObservableProperty]
    public partial string Name { get; set; } = name;

    /// <summary>
    /// 导航命令
    /// </summary>
    public IRelayCommand NavigateCommand { get; } = new RelayCommand(navigate);
}
