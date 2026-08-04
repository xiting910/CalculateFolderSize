using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CalculateFolderSize.UI.Shared.ViewModels;

/// <summary>
/// 结果窗口视图模型, 负责子项浏览、面包屑导航与排序
/// </summary>
public sealed partial class ResultWindowViewModel : ToastViewModelBase
{
    /// <summary>
    /// 大小列比较器, 严格按大小排序
    /// </summary>
    private static readonly IComparer<ResultItemViewModel> SizeComparer =
        Comparer<ResultItemViewModel>.Create((a, b) => a.Size.CompareTo(b.Size));

    /// <summary>
    /// 百分比列比较器, 严格按百分比排序
    /// </summary>
    private static readonly IComparer<ResultItemViewModel> PercentComparer =
        Comparer<ResultItemViewModel>.Create((a, b) => a.Percent.CompareTo(b.Percent));

    /// <summary>
    /// 文件夹数列比较器, 严格按文件夹数排序, 文件按 0 处理
    /// </summary>
    private static readonly IComparer<ResultItemViewModel> FolderCountComparer =
        Comparer<ResultItemViewModel>.Create((a, b) => (a.FolderCount ?? 0).CompareTo(b.FolderCount ?? 0));

    /// <summary>
    /// 文件数列比较器, 严格按文件数排序, 文件按 0 处理
    /// </summary>
    private static readonly IComparer<ResultItemViewModel> FileCountComparer =
        Comparer<ResultItemViewModel>.Create((a, b) => (a.FileCount ?? 0).CompareTo(b.FileCount ?? 0));

    /// <summary>
    /// 名称列比较器, 文件夹恒排在文件之前
    /// </summary>
    private readonly IComparer<ResultItemViewModel> NameComparer;

    /// <summary>
    /// 文件大小格式化器
    /// </summary>
    private readonly IFileSizeFormatter _formatter;

    /// <summary>
    /// 文件夹大小计算器
    /// </summary>
    private readonly IFolderSizeCalculator _calculator;

    /// <summary>
    /// 导航快照栈, 栈顶为当前目录的父目录
    /// </summary>
    private readonly Stack<Snapshot> _navigationStack = new();

    /// <summary>
    /// 根文件夹的大小信息, 用于顶部摘要显示
    /// </summary>
    private readonly FolderSize _rootSize;

    /// <summary>
    /// 当前目录快照
    /// </summary>
    private Snapshot _current;

    /// <summary>
    /// 创建结果窗口视图模型
    /// </summary>
    /// <param name="coreOptions">核心选项</param>
    /// <param name="formatter">文件大小格式化器</param>
    /// <param name="calculator">文件夹大小计算器</param>
    /// <param name="rootPath">根文件夹路径</param>
    /// <param name="rootSize">根文件夹大小</param>
    public ResultWindowViewModel(
        CoreOptions coreOptions,
        IFileSizeFormatter formatter,
        IFolderSizeCalculator calculator,
        string rootPath,
        FolderSize rootSize)
    {
        NameComparer = Comparer<ResultItemViewModel>.Create((a, b) => a.IsDirectory == b.IsDirectory
                ? coreOptions.PathComparer.Compare(a.Name, b.Name)
                : a.IsDirectory ? -1 : 1);

        _calculator = calculator;
        _formatter = formatter;
        RootPath = rootPath;
        _rootSize = rootSize;

        if (TryLoadChildren(rootPath, rootSize.TotalBytes, out var items))
        {
            _current = new(rootPath, rootSize.TotalBytes, items);
        }
        else
        {
            _current = new(rootPath, rootSize.TotalBytes, []);
            ShowFeedback("根文件夹的子项缓存已失效, 请重新扫描后再打开结果窗口");
        }
        Refresh();
    }

    /// <summary>
    /// 根文件夹路径
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// 窗口标题
    /// </summary>
    public string Title => $"计算结果: {RootPath}";

    /// <summary>
    /// 根文件夹总大小的格式化文本
    /// </summary>
    public string RootSizeText => _formatter.Format(_rootSize.TotalBytes);

    /// <summary>
    /// 根文件夹的子文件夹数
    /// </summary>
    public int RootFolderCount => _rootSize.FolderCount;

    /// <summary>
    /// 根文件夹的文件数
    /// </summary>
    public int RootFileCount => _rootSize.FileCount;

    /// <summary>
    /// 当前目录的子项列表
    /// </summary>
    public ObservableCollection<ResultItemViewModel> Items { get; } = [];

    /// <summary>
    /// 面包屑列表
    /// </summary>
    public ObservableCollection<BreadcrumbItemViewModel> Breadcrumbs { get; } = [];

    /// <summary>
    /// 下钻进入子文件夹
    /// </summary>
    /// <param name="item">要进入的文件夹子项</param>
    public void NavigateDown(ResultItemViewModel item)
    {
        if (!item.IsDirectory) { return; }
        if (!TryLoadChildren(item.Path, item.Size, out var items))
        {
            ShowFeedback("该文件夹的子项数据已不在缓存中, 请重新扫描该文件夹");
            return;
        }

        _navigationStack.Push(_current);
        _current = new(item.Path, item.Size, items);
        Refresh();
    }

    /// <summary>
    /// 返回父目录
    /// </summary>
    public void NavigateBack()
    {
        if (_navigationStack.Count == 0) { return; }
        _current = _navigationStack.Pop();
        Refresh();
    }

    /// <summary>
    /// 通过面包屑跳转到指定层级
    /// </summary>
    /// <param name="index">目标层级索引, 0 为根目录</param>
    public void NavigateToBreadcrumb(int index)
    {
        for (var i = _navigationStack.Count; i > index; i--)
        {
            _current = _navigationStack.Pop();
        }
        Refresh();
    }

    /// <summary>
    /// 以系统默认方式打开指定文件, 失败时短暂提示
    /// </summary>
    /// <param name="item">文件子项</param>
    public void OpenFile(ResultItemViewModel item)
    {
        if (!SystemOpener.TryOpen(item.Path, out var errorMessage))
        {
            ShowFeedback(errorMessage);
        }
    }

    /// <summary>
    /// 用系统文件资源管理器打开当前文件夹, 失败时短暂提示
    /// </summary>
    [RelayCommand]
    private void OpenInExplorer()
    {
        if (!SystemOpener.TryOpen(_current.Path, out var errorMessage))
        {
            ShowFeedback(errorMessage);
        }
    }

    /// <summary>
    /// 对子项列表按指定列排序
    /// </summary>
    /// <param name="key">排序列标识</param>
    /// <param name="ascending">是否升序</param>
    public void ApplySort(string key, bool ascending)
    {
        var comparer = key switch
        {
            nameof(ResultItemViewModel.Name) => NameComparer,
            nameof(ResultItemViewModel.Size) => SizeComparer,
            nameof(ResultItemViewModel.Percent) => PercentComparer,
            nameof(ResultItemViewModel.FolderCount) => FolderCountComparer,
            nameof(ResultItemViewModel.FileCount) => FileCountComparer,
            _ => Comparer<ResultItemViewModel>.Default
        };

        var list = _current.Items.ToList();
        list.Sort(comparer);
        if (!ascending)
        {
            list.Reverse();
        }

        Items.Clear();
        foreach (var item in list)
        {
            Items.Add(item);
        }
    }

    /// <summary>
    /// 刷新子项列表与面包屑
    /// </summary>
    private void Refresh()
    {
        Items.Clear();
        foreach (var item in _current.Items)
        {
            Items.Add(item);
        }
        BuildBreadcrumbs();
    }

    /// <summary>
    /// 根据当前路径重建面包屑
    /// </summary>
    private void BuildBreadcrumbs()
    {
        Breadcrumbs.Clear();
        var segments = _current.Path.Split(
            Constants.AllowedDirectorySeparators,
            StringSplitOptions.RemoveEmptyEntries
        );
        for (var i = 0; i < segments.Length; i++)
        {
            var index = i;
            Breadcrumbs.Add(new(segments[i], () => NavigateToBreadcrumb(index)));
        }
    }

    /// <summary>
    /// 从缓存加载指定文件夹的子项并构建视图模型列表
    /// </summary>
    /// <param name="path">文件夹路径</param>
    /// <param name="totalBytes">该文件夹总字节数, 用于计算子项百分比</param>
    /// <param name="items">输出参数, 子项视图模型列表</param>
    /// <returns><see langword="true"/> 如果成功从缓存加载</returns>
    private bool TryLoadChildren(string path, long totalBytes, out List<ResultItemViewModel> items)
    {
        if (_calculator.TryGetFolderChildren(path, out var children))
        {
            items = [.. children.Select(c => new ResultItemViewModel(_formatter, c, totalBytes))];
            return true;
        }

        items = [];
        return false;
    }

    /// <summary>
    /// 导航快照
    /// </summary>
    /// <param name="Path">文件夹路径</param>
    /// <param name="TotalBytes">文件夹总字节数</param>
    /// <param name="Items">子项视图模型列表</param>
    private sealed record Snapshot(string Path, long TotalBytes, List<ResultItemViewModel> Items);
}
