using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CalculateFolderSize.UI.Shared.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFolderSize.UI.Shared.Views;

/// <summary>
/// 主视图
/// </summary>
public sealed partial class MainView : UserControl
{
    /// <summary>
    /// 已点击过的排序表头的基础文本, 用于切换排序列时清除箭头
    /// </summary>
    private readonly Dictionary<TextBlock, string> _headerBaseTexts = [];

    /// <summary>
    /// 当前排序列标识
    /// </summary>
    private string _sortKey = string.Empty;

    /// <summary>
    /// 当前是否升序
    /// </summary>
    private bool _sortAscending = true;

    /// <summary>
    /// 创建主视图
    /// </summary>
    public MainView()
    {
        InitializeComponent();
        InputPathsListBox.AddHandler(PointerPressedEvent, OnPathListPointerPressed, RoutingStrategies.Tunnel);
        HistoriesListBox.AddHandler(PointerPressedEvent, OnPathListPointerPressed, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// 历史记录多选变化时同步到视图模型
    /// </summary>
    /// <param name="sender">ListBox</param>
    /// <param name="e">选中变化事件参数</param>
    private void OnHistoriesSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SelectedHistories = HistoriesListBox.SelectedItems?.Cast<string>().ToArray() ?? [];
        }
    }

    /// <summary>
    /// 点击列表行时将路径复制到剪贴板, 事件源可能是行内任意元素
    /// </summary>
    /// <param name="sender">ListBox</param>
    /// <param name="e">指针事件参数</param>
    private void OnPathListPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Control { DataContext: string path } && DataContext is MainViewModel viewModel)
        {
            viewModel.CopyPathCommand.Execute(path);
        }
    }

    /// <summary>
    /// 双击任务行时查看结果, 无法查看时显示短暂提示
    /// </summary>
    /// <param name="sender">DataGrid</param>
    /// <param name="e">单元格指针按下事件参数</param>
    private void OnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (e.PointerPressedEventArgs.ClickCount != 2)
        {
            return;
        }
        if (e.Row.DataContext is CalculateTaskViewModel task && DataContext is MainViewModel viewModel)
        {
            if (task.CanOpenResult)
            {
                task.OpenResult();
            }
            else
            {
                viewModel.ShowToast(task.OpenResultHint);
            }
        }
    }

    /// <summary>
    /// 点击可排序表头时切换排序方向并刷新箭头指示
    /// </summary>
    /// <param name="sender">表头文本</param>
    /// <param name="e">指针事件参数</param>
    private void OnSortHeaderPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not TextBlock { Tag: string key } header || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (!_headerBaseTexts.ContainsKey(header))
        {
            var baseText = header.Text ?? string.Empty;
            _headerBaseTexts[header] = baseText;
        }

        if (_sortKey == key)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortKey = key;
            _sortAscending = true;
        }

        foreach (var (headerText, baseName) in _headerBaseTexts)
        {
            headerText.Text = (string?)headerText.Tag == key
                ? $"{baseName} {(_sortAscending ? "▲" : "▼")}"
                : baseName;
        }

        viewModel.ApplySort(key, _sortAscending);
    }
}
