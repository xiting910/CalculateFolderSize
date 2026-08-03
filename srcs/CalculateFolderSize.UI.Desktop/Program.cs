using Avalonia;
using CalculateFolderSize.Core;
using CalculateFolderSize.UI.Shared;
using CalculateFolderSize.UI.Shared.Models;
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
    /// 获取日志级别的 Key
    /// </summary>
    private const string LogLevelKey = $"{nameof(UI)}:{nameof(UIOptions.Level)}";

    /// <summary>
    /// 最新日志文件路径
    /// </summary>
    private static readonly string LatestLogFilePath = Path.Combine(
        Constants.LogsDirectory,
        Constants.LatestLogFileName
    );

    /// <summary>
    /// 应用程序入口点
    /// </summary>
    /// <param name="args">命令行参数</param>
    [STAThread]
    private static int Main(string[] args)
    {
        RotateLogFiles();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Constants.AppDataDirectory, Constants.SettingsFileName), optional: true)
            .Build();

        if (!Enum.TryParse<LogLevel>(configuration[LogLevelKey], out var level))
        {
            level = LogLevel.Information;
        }

        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddFile(LatestLogFilePath, options => options.MinLevel = level))
            .AddSingleton<IConfiguration>(configuration)
            .AddCore()
            .AddUIShared()
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
            if (File.Exists(LatestLogFilePath))
            {
                var newLogFileName = $"{DateTime.Now:yyyy-MM-dd_HHmmss}{Constants.LogFileExtension}";
                File.Move(LatestLogFilePath, Path.Combine(Constants.LogsDirectory, newLogFileName));
            }

            var oldFiles = logsDirInfo
                .GetFiles($"*{Constants.LogFileExtension}", SearchOption.TopDirectoryOnly)
                .Select(f => f.FullName)
                .Where(path => !path.Equals(LatestLogFilePath, StringComparison.OrdinalIgnoreCase))
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
