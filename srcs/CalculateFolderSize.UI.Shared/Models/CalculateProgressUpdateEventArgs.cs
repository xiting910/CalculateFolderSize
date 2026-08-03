using CalculateFolderSize.Core.Models;
using System;

namespace CalculateFolderSize.UI.Shared.Models;

/// <summary>
/// 计算进度更新事件参数
/// </summary>
/// <param name="progressReport">当前进度报告</param>
/// <param name="speedBytesPerSecond">当前计算速度</param>
public sealed class CalculateProgressUpdateEventArgs(
    ProgressReport progressReport,
    double speedBytesPerSecond
) : EventArgs
{
    /// <summary>
    /// 当前进度报告
    /// </summary>
    public ProgressReport ProgressReport { get; } = progressReport;

    /// <summary>
    /// 当前计算速度, 单位: 字节/秒
    /// </summary>
    public double SpeedBytesPerSecond { get; } = speedBytesPerSecond;
}
