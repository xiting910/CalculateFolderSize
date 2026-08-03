using Avalonia.Controls;
using Avalonia.Input;
using CalculateFolderSize.UI.Shared.ViewModels;
using System.Collections.Generic;

namespace CalculateFolderSize.UI.Shared.Views;

/// <summary>
/// 结果窗口
/// </summary>
public partial class ResultWindow : Window
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
    /// 创建结果窗口
    /// </summary>
    public ResultWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 触摸长按控件时打开其悬浮提示, 抬手或取消时关闭, 桌面端仍由鼠标悬浮触发
    /// </summary>
    /// <param name="sender">提示控件</param>
    /// <param name="e">长按事件参数</param>
    private void OnControlHolding(object? sender, HoldingRoutedEventArgs e)
    {
        ToolTipHelper.ToggleByHolding(sender, e);
    }

    /// <summary>
    /// 双击行时按类型处理: 文件夹下钻进入, 文件以系统默认方式打开
    /// </summary>
    /// <param name="sender">DataGrid</param>
    /// <param name="e">单元格指针按下事件参数</param>
    private void OnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (e.PointerPressedEventArgs.ClickCount != 2)
        {
            return;
        }
        if (e.Row.DataContext is ResultItemViewModel item && DataContext is ResultWindowViewModel viewModel)
        {
            if (item.IsDirectory)
            {
                viewModel.NavigateDown(item);
            }
            else
            {
                viewModel.OpenFile(item);
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
        if (sender is not TextBlock { Tag: string key } header || DataContext is not ResultWindowViewModel viewModel)
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
