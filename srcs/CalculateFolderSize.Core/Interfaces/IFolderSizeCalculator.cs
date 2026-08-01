using CalculateFolderSize.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace CalculateFolderSize.Core.Interfaces;

/// <summary>
/// 文件夹大小计算器接口
/// </summary>
public interface IFolderSizeCalculator : INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// 获取缓存文件夹数
    /// </summary>
    /// <exception cref="ObjectDisposedException">当对象已被释放时抛出</exception>
    int CacheCount { get; }

    /// <summary>
    /// 清理缓存
    /// </summary>
    /// <exception cref="ObjectDisposedException">当对象已被释放时抛出</exception>
    void ClearCache();

    /// <summary>
    /// 获取指定文件夹路径的 <see cref="FolderSize"/> 对象
    /// </summary>
    /// <param name="folderPath">指定的文件夹路径</param>
    /// <param name="progress">进度报告</param>
    /// <param name="token">取消令牌</param>
    /// <returns>文件夹大小对象, 如果文件夹不存在则返回 <see langword="null"/></returns>
    /// <exception cref="ObjectDisposedException">当对象已被释放时抛出</exception>
    /// <exception cref="OperationCanceledException">当操作被取消时抛出</exception>
    FolderSize? GetFromFolder(
        string folderPath,
        IProgress<ProgressReport>? progress = null,
        CancellationToken token = default
    );

    /// <summary>
    /// 尝试获取指定文件夹路径的子项列表
    /// </summary>
    /// <param name="folderPath">指定的文件夹路径</param>
    /// <param name="children">输出参数, 如果成功则返回文件夹子项列表</param>
    /// <returns><see langword="true"/> 如果成功获取子项列表, 否则返回 <see langword="false"/></returns>
    /// <exception cref="ObjectDisposedException">当对象已被释放时抛出</exception>
    bool TryGetFolderChildren(
        string folderPath,
        [MaybeNullWhen(false)] out IReadOnlyList<FolderChild> children
    );
}
