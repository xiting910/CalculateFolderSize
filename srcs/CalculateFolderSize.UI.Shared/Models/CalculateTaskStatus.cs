using System.ComponentModel;

namespace CalculateFolderSize.UI.Shared.Models;

/// <summary>
/// 计算任务状态
/// </summary>
public enum CalculateTaskStatus
{
    /// <summary>
    /// 运行中
    /// </summary>
    [Description("运行中")]
    Running,

    /// <summary>
    /// 已完成
    /// </summary>
    [Description("已完成")]
    Completed,

    /// <summary>
    /// 已取消
    /// </summary>
    [Description("已取消")]
    Cancelled,

    /// <summary>
    /// 目录不存在
    /// </summary>
    [Description("目录不存在")]
    DirectoryNotFound,

    /// <summary>
    /// 失败
    /// </summary>
    [Description("失败")]
    Failed
}
