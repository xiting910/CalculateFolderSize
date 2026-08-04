using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CalculateFolderSize.UI.Shared;

/// <summary>
/// 系统打开辅助类, 按平台用默认方式打开文件或文件夹
/// </summary>
public static class SystemOpener
{
    /// <summary>
    /// 交给系统默认方式打开文件或文件夹, 桌面平台用系统命令, 安卓端暂不支持
    /// </summary>
    /// <param name="path">本地路径</param>
    /// <param name="errorMessage">失败时的错误信息</param>
    /// <returns><see langword="true"/> 如果成功启动</returns>
    public static bool TryOpen(string path, [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = default;
        try
        {
            if (OperatingSystem.IsAndroid())
            {
                errorMessage = "安卓端暂不支持打开文件或文件夹";
                return false;
            }
            if (OperatingSystem.IsWindows())
            {
                _ = Process.Start("explorer.exe", $"\"{path}\"");
                return true;
            }
            if (OperatingSystem.IsMacOS())
            {
                _ = Process.Start("open", $"\"{path}\"");
                return true;
            }
            _ = Process.Start("xdg-open", $"\"{path}\"");
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"无法打开: {path}\n{ex.Message}";
            return false;
        }
    }
}
