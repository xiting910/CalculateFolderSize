using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CalculateFolderSize.UI.Shared.Views;
using System;

namespace CalculateFolderSize.UI.Shared;

/// <summary>
/// 主窗口提供器, 从应用程序生命周期获取当前主窗口
/// </summary>
internal sealed class MainWindowProvider : IMainWindowProvider
{
    /// <inheritdoc/>
    public MainWindow MainWindow =>
        (Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
            ISingleViewApplicationLifetime singleView => singleView.MainView,
            _ => null
        } as MainWindow) ?? throw new InvalidOperationException("主窗口尚未创建");
}
