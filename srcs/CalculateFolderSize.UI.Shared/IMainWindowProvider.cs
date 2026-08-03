using CalculateFolderSize.UI.Shared.Views;
using System;

namespace CalculateFolderSize.UI.Shared;

/// <summary>
/// 主窗口提供器接口
/// </summary>
public interface IMainWindowProvider
{
    /// <summary>
    /// 当前主窗口
    /// </summary>
    /// <exception cref="InvalidOperationException">主窗口尚未创建时抛出</exception>
    MainWindow MainWindow { get; }
}
