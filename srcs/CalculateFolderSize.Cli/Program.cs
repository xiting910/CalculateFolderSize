using CalculateFolderSize.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Threading.Tasks;

namespace CalculateFolderSize.Cli;

/// <summary>
/// 程序入口点
/// </summary>
file static class Program
{
    /// <summary>
    /// 程序入口点
    /// </summary>
    private static async Task Main()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var app = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddCore()
            .AddSingleton<CliOptions>()
            .AddSingleton(AnsiConsole.Console)
            .AddSingleton<IPathNormalizer, PathNormalizer>()
            .AddSingleton<IUserInputProcessor, UserInputProcessor>()
            .AddSingleton<App>()
            .BuildServiceProvider()
            .GetRequiredService<App>();

        await app.RunAsync();
    }
}
