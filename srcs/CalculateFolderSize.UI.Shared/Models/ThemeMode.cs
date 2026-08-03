using System.ComponentModel;

namespace CalculateFolderSize.UI.Shared.Models;

/// <summary>
/// UI 层主题模式
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// 跟随系统
    /// </summary>
    [Description("跟随系统")]
    System,

    /// <summary>
    /// 浅色
    /// </summary>
    [Description("浅色")]
    Light,

    /// <summary>
    /// 深色
    /// </summary>
    [Description("深色")]
    Dark
}
