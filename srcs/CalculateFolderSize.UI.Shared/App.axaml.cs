using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using CalculateFolderSize.UI.Shared.Models;
using CalculateFolderSize.UI.Shared.ViewModels;
using CalculateFolderSize.UI.Shared.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace CalculateFolderSize.UI.Shared;

/// <summary>
/// 应用程序类
/// </summary>
public sealed partial class App : Application
{
    /// <summary>
    /// 服务容器, 由平台入口在启动时注入
    /// </summary>
    /// <exception cref="InvalidOperationException">服务容器未初始化</exception>
    public static IServiceProvider Services
    {
        get => field ?? throw new InvalidOperationException("服务容器未初始化, 请先在平台入口设置 App.Services");
        set;
    }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        try { _ = Directory.CreateDirectory(Constants.AppDataDirectory); } catch { }

        var uiOptions = Services.GetRequiredService<UIOptions>();
        Current?.RequestedThemeVariant = uiOptions.Theme switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new ShellWindow
            {
                DataContext = Services.GetRequiredService<ShellViewModel>()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new ShellView
            {
                DataContext = Services.GetRequiredService<ShellViewModel>()
            };
        }
    }
}
