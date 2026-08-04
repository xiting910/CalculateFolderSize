using Avalonia;
using CalculateFolderSize.Core;
using CalculateFolderSize.UI.Shared;
using CalculateFolderSize.UI.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using System;
using System.IO;
using System.Linq;

namespace CalculateFolderSize.UI.Desktop;

/// <summary>
/// 程序入口类
/// </summary>
file static class Program
{
    /// <summary>
    /// 应用程序入口点
    /// </summary>
    /// <param name="args">命令行参数</param>
    [STAThread]
    private static int Main(string[] args)
    {
        RotateLogFiles();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Constants.SettingsFilePath, optional: true)
            .Build();

        if (!Enum.TryParse<LogLevel>(configuration[Constants.LogLevelKey], out var level))
        {
            level = LogLevel.Information;
        }

        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddFile(Constants.LatestLogFilePath, o => o.MinLevel = level))
            .AddSingleton<IConfiguration>(configuration)
            .AddCore()
            .AddUIShared()
            .AddSingleton<IStorageAccessService, DesktopStorageAccessService>()
            .BuildServiceProvider();

        App.Services = services;
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// 轮转日志: 上次的 latest.log 重命名为当前时间, 只保留 <see cref="Constants.MaxLogFiles"/> 个最新日志文件
    /// </summary>
    private static void RotateLogFiles()
    {
        try
        {
            var logsDirInfo = Directory.CreateDirectory(Constants.LogsDirectory);
            if (File.Exists(Constants.LatestLogFilePath))
            {
                var newLogFileName = $"{DateTime.Now:yyyy-MM-dd_HHmmss}{Constants.LogFileExtension}";
                File.Move(Constants.LatestLogFilePath, Path.Combine(Constants.LogsDirectory, newLogFileName));
            }

            var oldFiles = logsDirInfo
                .GetFiles($"*{Constants.LogFileExtension}", SearchOption.TopDirectoryOnly)
                .Select(f => f.FullName)
                .Where(path => !path.Equals(Constants.LatestLogFilePath, StringComparison.OrdinalIgnoreCase))
                .OrderDescending()
                .Skip(Constants.MaxLogFiles - 1);

            foreach (var old in oldFiles)
            {
                File.Delete(old);
            }
        }
        catch { }
    }
}
