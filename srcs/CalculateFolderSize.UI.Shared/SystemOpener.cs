using Avalonia.Platform.Storage;
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
    /// 交给系统默认方式打开文件或文件夹, 安卓经系统 Launcher 发起 Intent, 桌面平台用系统命令
    /// </summary>
    /// <param name="path">本地路径或安卓 content URI</param>
    /// <param name="launcher">安卓端的系统 Launcher</param>
    /// <param name="errorMessage">失败时的错误信息</param>
    /// <returns><see langword="true"/> 如果成功启动</returns>
    public static bool TryOpen(
        string path,
        ILauncher? launcher,
        [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = default;
        try
        {
            if (OperatingSystem.IsAndroid())
            {
                if (launcher is null)
                {
                    errorMessage = "系统 Launcher 不可用, 无法打开";
                    return false;
                }
                _ = launcher.LaunchUriAsync(new Uri(path));
                return true;
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
