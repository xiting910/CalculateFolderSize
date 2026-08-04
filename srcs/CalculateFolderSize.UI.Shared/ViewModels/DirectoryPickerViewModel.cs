using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CalculateFolderSize.UI.Shared.ViewModels;

/// <summary>
/// 目录选择视图模型, 供安卓端浏览共享存储并选择要计算的文件夹
/// </summary>
public sealed partial class DirectoryPickerViewModel : ObservableObject
{
    /// <summary>
    /// 文件系统
    /// </summary>
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// 全局短暂提示视图模型
    /// </summary>
    private readonly ToastViewModel _toast;

    /// <summary>
    /// 父目录栈, 栈顶为当前目录的父目录
    /// </summary>
    private readonly Stack<string> _upStack = new();

    /// <summary>
    /// 当前目录
    /// </summary>
    [ObservableProperty]
    public partial string CurrentPath { get; set; }

    /// <summary>
    /// 是否可以返回上一级
    /// </summary>
    [ObservableProperty]
    public partial bool CanGoUp { get; set; }

    /// <summary>
    /// 当前目录的子文件夹列表
    /// </summary>
    public ObservableCollection<DirectoryEntry> Directories { get; } = [];

    /// <summary>
    /// 创建目录选择视图模型
    /// </summary>
    /// <param name="fileSystem">文件系统</param>
    /// <param name="toastViewModel">全局短暂提示视图模型</param>
    public DirectoryPickerViewModel(IFileSystem fileSystem, ToastViewModel toastViewModel)
    {
        _fileSystem = fileSystem;
        _toast = toastViewModel;
        CurrentPath = Constants.AndroidSharedStorageRoot;
        Refresh();
    }

    /// <summary>
    /// 进入子文件夹
    /// </summary>
    /// <param name="directory">要进入的子文件夹</param>
    public void Enter(DirectoryEntry directory)
    {
        _upStack.Push(CurrentPath);
        CurrentPath = directory.FullName;
        Refresh();
    }

    /// <summary>
    /// 选中目录的事件, 由壳视图模型处理
    /// </summary>
    public event Action<string>? DirectorySelected;

    /// <summary>
    /// 取消选择的事件, 由壳视图模型处理
    /// </summary>
    public event Action? CancelRequested;

    /// <summary>
    /// 确认选择当前目录
    /// </summary>
    public void ConfirmSelection()
    {
        DirectorySelected?.Invoke(CurrentPath);
    }

    /// <summary>
    /// 取消选择
    /// </summary>
    public void Cancel()
    {
        CancelRequested?.Invoke();
    }

    /// <summary>
    /// 返回上一级目录
    /// </summary>
    [RelayCommand]
    private void GoUp()
    {
        if (_upStack.Count == 0) { return; }
        CurrentPath = _upStack.Pop();
        Refresh();
    }

    /// <summary>
    /// 刷新当前目录的子文件夹列表
    /// </summary>
    private void Refresh()
    {
        CanGoUp = _upStack.Count > 0;
        Directories.Clear();
        try
        {
            foreach (var directory in _fileSystem.EnumerateDirectories(CurrentPath))
            {
                Directories.Add(directory);
            }
        }
        catch (Exception ex)
        {
            _toast.Show($"无法读取目录: {ex.Message}");
        }
    }
}
