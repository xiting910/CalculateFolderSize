using Avalonia.Controls;
using System;

namespace CalculateFolderSize.UI.Shared;

/// <summary>
/// 顶层视图提供器接口
/// </summary>
public interface ITopLevelProvider
{
    /// <summary>
    /// 当前顶层视图
    /// </summary>
    /// <exception cref="NotSupportedException">不支持的应用程序生命周期类型时抛出</exception>
    /// <exception cref="InvalidOperationException">顶层视图尚未创建时抛出</exception>
    TopLevel TopLevel { get; }
}
