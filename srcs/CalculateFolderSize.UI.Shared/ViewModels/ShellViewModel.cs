using CalculateFolderSize.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CalculateFolderSize.UI.Shared.ViewModels;

/// <summary>
/// 壳视图模型, 负责主视图与结果栈/设置抽屉/目录选择器之间的导航
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    /// <summary>
    /// 结果视图模型字典, 按路径去重
    /// </summary>
    private readonly Dictionary<string, ResultViewModel> _results;

    /// <summary>
    /// 目录选择回调, 选中路径时调用
    /// </summary>
    private Action<string>? _directoryPickCallback;

    /// <summary>
    /// 主视图模型
    /// </summary>
    public MainViewModel Main { get; }

    /// <summary>
    /// 全局短暂提示视图模型
    /// </summary>
    public ToastViewModel Toast { get; }

    /// <summary>
    /// 结果视图栈, 栈顶为当前可见的结果视图
    /// </summary>
    public ObservableCollection<ResultViewModel> ResultStack { get; } = [];

    /// <summary>
    /// 当前可见的结果视图
    /// </summary>
    public ResultViewModel? CurrentResult => ResultStack.Count > 0 ? ResultStack[^1] : null;

    /// <summary>
    /// 是否存在结果视图
    /// </summary>
    public bool HasResult => ResultStack.Count > 0;

    /// <summary>
    /// 设置视图模型, 打开设置抽屉时创建
    /// </summary>
    [ObservableProperty]
    public partial SettingsViewModel? Settings { get; set; }

    /// <summary>
    /// 设置抽屉是否打开
    /// </summary>
    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }

    /// <summary>
    /// 目录选择视图模型, 打开目录选择器时创建
    /// </summary>
    [ObservableProperty]
    public partial DirectoryPickerViewModel? DirectoryPicker { get; set; }

    /// <summary>
    /// 目录选择器是否打开
    /// </summary>
    [ObservableProperty]
    public partial bool IsDirectoryPickerOpen { get; set; }

    /// <summary>
    /// 创建壳视图模型
    /// </summary>
    /// <param name="main">主视图模型</param>
    /// <param name="toast">全局短暂提示视图模型</param>
    /// <param name="coreOptions">Core 选项</param>
    public ShellViewModel(
        MainViewModel main,
        ToastViewModel toast,
        CoreOptions coreOptions)
    {
        Main = main;
        Toast = toast;
        _results = new(coreOptions.PathComparer);

        Main.SettingsRequested += OpenSettings;
        Main.DirectoryPickerRequested += () => OpenDirectoryPicker(OnDirectoryPicked);
    }

    /// <summary>
    /// 打开结果视图
    /// </summary>
    /// <param name="path">根文件夹路径</param>
    /// <param name="size">根文件夹大小</param>
    public void OpenResult(string path, FolderSize size)
    {
        if (_results.TryGetValue(path, out var existing))
        {
            _ = ResultStack.Remove(existing);
            ResultStack.Add(existing);
            NotifyCurrentResultChanged();
            return;
        }

        var viewModel = ActivatorUtilities.CreateInstance<ResultViewModel>(App.Services, path, size);
        viewModel.CloseRequested += CloseResult;
        _results[path] = viewModel;
        ResultStack.Add(viewModel);
        NotifyCurrentResultChanged();
    }

    /// <summary>
    /// 关闭结果视图并返回上一层
    /// </summary>
    /// <param name="viewModel">要关闭的结果视图</param>
    public void CloseResult(ResultViewModel viewModel)
    {
        if (ResultStack.Remove(viewModel))
        {
            viewModel.CloseRequested -= CloseResult;
            _ = _results.Remove(viewModel.RootPath);
            NotifyCurrentResultChanged();
        }
    }

    /// <summary>
    /// 打开设置抽屉
    /// </summary>
    private void OpenSettings()
    {
        Settings = App.Services.GetRequiredService<SettingsViewModel>();
        Settings.CloseRequested += CloseSettings;
        IsSettingsOpen = true;
    }

    /// <summary>
    /// 关闭设置抽屉
    /// </summary>
    public void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    /// <summary>
    /// 打开目录选择器, 选中路径后回调
    /// </summary>
    /// <param name="onPicked">选中路径回调</param>
    public void OpenDirectoryPicker(Action<string> onPicked)
    {
        _directoryPickCallback = onPicked;
        DirectoryPicker = ActivatorUtilities.CreateInstance<DirectoryPickerViewModel>(App.Services);
        DirectoryPicker.DirectorySelected += OnDirectorySelected;
        DirectoryPicker.CancelRequested += () => IsDirectoryPickerOpen = false;
        IsDirectoryPickerOpen = true;
    }

    /// <summary>
    /// 目录选择完成回调
    /// </summary>
    /// <param name="path">选中的路径</param>
    private void OnDirectorySelected(string path)
    {
        IsDirectoryPickerOpen = false;
        _directoryPickCallback?.Invoke(path);
    }

    /// <summary>
    /// 目录选择回调, 将选中的路径加入输入列表
    /// </summary>
    /// <param name="path">选中的路径</param>
    private void OnDirectoryPicked(string path)
    {
        Main.AddInputPath(path);
    }

    /// <summary>
    /// 通知结果栈相关属性变化
    /// </summary>
    private void NotifyCurrentResultChanged()
    {
        OnPropertyChanged(nameof(CurrentResult));
        OnPropertyChanged(nameof(HasResult));
    }
}
