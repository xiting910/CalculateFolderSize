using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;

namespace CalculateFolderSize.UI.Shared;

/// <summary>
/// 顶层视图提供器, 从应用程序生命周期获取当前顶层视图
/// </summary>
internal sealed class TopLevelProvider : ITopLevelProvider
{
    /// <inheritdoc/>
    public TopLevel TopLevel =>
        Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
            ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(singleView.MainView),
            _ => throw new NotSupportedException("不支持的应用程序生命周期类型")
        } ?? throw new InvalidOperationException("顶层视图尚未创建");
}
