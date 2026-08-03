using CalculateFolderSize.Core.Models;
using CalculateFolderSize.UI.Shared.Models;
using System;

namespace CalculateFolderSize.UI.Shared.Interfaces;

/// <summary>
/// 计算进度接口
/// </summary>
public interface ICalculateProgress : IProgress<ProgressReport>
{
    /// <summary>
    /// 计算进度更新事件
    /// </summary>
    event EventHandler<CalculateProgressUpdateEventArgs>? ProgressUpdated;

    /// <summary>
    /// 设置进度为已完成以立刻触发 <see cref="ProgressUpdated"/> 事件避免最后一次报告被节流掉
    /// </summary>
    void Complete();
}
