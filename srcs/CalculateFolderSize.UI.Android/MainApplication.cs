using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using CalculateFolderSize.Core;
using CalculateFolderSize.UI.Shared;
using CalculateFolderSize.UI.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using System;

namespace CalculateFolderSize.UI.Android;

/// <summary>
/// 主应用程序类
/// </summary>
[Application]
public class MainApplication : AvaloniaAndroidApplication<App>
{
    /// <summary>
    /// 初始化一个新的 <see cref="MainApplication"/> 实例
    /// </summary>
    /// <param name="javaReference">Java 参考</param>
    /// <param name="transfer">JNI 处理所有权</param>
    protected MainApplication(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer) { }

    /// <summary>
    /// 自定义应用程序构建器
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <returns>自定义后的应用程序构建器</returns>
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Constants.SettingsFilePath, optional: true)
            .Build();

        if (!Enum.TryParse<LogLevel>(configuration[Constants.LogLevelKey], out var level))
        {
            level = LogLevel.Information;
        }

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddCore()
            .AddUIShared()
            .AddSingleton<IStorageAccessService, StorageAccessService>();

        App.Services = (AndroidEnvironment.IsExternalStorageManager
            ? services.AddLogging(b => b.AddFile(Constants.LatestLogFilePath, o => o.MinLevel = level))
            : services.AddLogging())
            .BuildServiceProvider();

        return base.CustomizeAppBuilder(builder).WithInterFont();
    }
}
