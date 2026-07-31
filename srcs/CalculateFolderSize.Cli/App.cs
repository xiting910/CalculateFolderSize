using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFolderSize.Cli;

/// <summary>
/// 应用程序主类
/// </summary>
/// <param name="_options">Cli 配置</param>
/// <param name="_console">控制台实例</param>
/// <param name="_formatter">文件大小格式化器</param>
/// <param name="_pathNormalizer">路径标准化器</param>
/// <param name="_calculator">文件夹大小计算器</param>
internal sealed class App(
    CliOptions _options,
    IAnsiConsole _console,
    IFileSizeFormatter _formatter,
    IPathNormalizer _pathNormalizer,
    IFolderSizeCalculator _calculator)
{
    /// <summary>
    /// 等待用户按键信息
    /// </summary>
    private const string WaitForKeyMessage = "按任意键继续...";

    /// <summary>
    /// 运行应用程序主循环
    /// </summary>
    public async Task RunAsync()
    {
        var prompt1 = "请输入要计算大小的文件夹路径, 输入多个路径时用双引号包裹";
        var prompt2 = $"输入 {_options.ExitCommand} 退出, {_options.ClearCacheCommand} 清理缓存";

        while (true)
        {
            _console.Clear();
            _console.MarkupLine($"[teal]{Markup.Escape(prompt1)}[/]");
            _console.MarkupLine($"[teal]{Markup.Escape(prompt2)}[/]");

            var input = _console.Prompt(new TextPrompt<string>(string.Empty).AllowEmpty());
            if (string.IsNullOrWhiteSpace(input)) { continue; }

            if (input.Equals(_options.ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            if (input.Equals(_options.ClearCacheCommand, StringComparison.OrdinalIgnoreCase))
            {
                _calculator.ClearCache();
                _console.MarkupLine("\n[lime]缓存已清理[/]\n");
                _console.Markup(WaitForKeyMessage);
                _ = _console.Input.ReadKey(true);
                continue;
            }

            var paths = ParsePaths(input.Trim());
            if (paths.Count == 0)
            {
                _console.MarkupLine("\n[yellow]未输入有效的文件夹路径[/]\n");
                _console.Markup(WaitForKeyMessage);
                _ = _console.Input.ReadKey(true);
                continue;
            }

            var oldCacheCount = _calculator.CacheCount;
            _console.MarkupLine(
                $"\n[navy]正在并行计算 [/][olive]{paths.Count}[/]" +
                $"[navy] 个文件夹的大小 (已缓存 [/][olive]{oldCacheCount}[/]" +
                $"[navy] 个文件夹计算结果):[/]\n");
            _console.MarkupLine($"[silver]{Markup.Escape(string.Join("\n", paths.Select(p => $"- {p}")))}[/]");
            _console.MarkupLine(string.Empty);

            var stopwatch = Stopwatch.StartNew();

            List<FolderSize> validResults = [];
            List<(string Path, Task<FolderSize?> Task)> tasks = [];
            foreach (var path in paths)
            {
                tasks.Add((path, Task.Run(() => _calculator.GetFromFolder(path))));
            }

            while (tasks.Count > 0)
            {
                var finished = await Task.WhenAny(tasks.Select(t => t.Task));
                var item = tasks.First(t => t.Task == finished);
                _ = tasks.Remove(item);

                var result = await finished;
                if (result is not null)
                {
                    validResults.Add(result);
                    _console.MarkupLine($"[lime]{Markup.Escape(item.Path)} 计算完成[/]");
                }
                else
                {
                    _console.MarkupLine($"[yellow]{Markup.Escape(item.Path)} 目录不存在[/]");
                }
            }
            stopwatch.Stop();

            if (validResults.Count == 0)
            {
                _console.MarkupLine("\n[yellow]没有找到任何有效的文件夹[/]\n");
            }
            else
            {
                _console.MarkupLine(string.Empty);
                validResults.Sort((a, b) => b.TotalBytes.CompareTo(a.TotalBytes));

                var maxPathLength = validResults.Max(r => r.Path.Length);
                var maxFileCountLength = validResults.Max(r => r.FileCount.ToString().Length);
                var maxFolderCountLength = validResults.Max(r => r.FolderCount.ToString().Length);
                var maxBytesLength = validResults.Max(r => r.TotalBytes.ToString().Length);

                foreach (var result in validResults)
                {
                    WriteResult(result, maxPathLength, maxFileCountLength, maxFolderCountLength, maxBytesLength);
                }

                _console.MarkupLine(
                    $"\n[lime]计算完成, 总耗时: [/][olive]{stopwatch.Elapsed.TotalSeconds:0.##}[/]" +
                    $"[lime] 秒, 新缓存 [/][olive]{_calculator.CacheCount - oldCacheCount}[/]" +
                    $"[lime] 个文件夹[/]\n");
            }

            // 检查是否有错误路径, 询问用户是否查看
            var resultsWithErrors = validResults.Where(r => r.ErrorPaths.Count > 0).ToArray();
            if (resultsWithErrors.Length > 0)
            {
                _console.Markup(
                    $"[teal]共 {resultsWithErrors.Length} 个文件夹存在访问错误, 是否查看错误路径? (y/n):[/]");
                var showErrorsInput = _console.Prompt(new TextPrompt<string>("").AllowEmpty());
                _console.MarkupLine(string.Empty);
                if (char.TryParse(showErrorsInput, out var showErrorsChar) && showErrorsChar is 'y' or 'Y')
                {
                    foreach (var result in resultsWithErrors)
                    {
                        _console.MarkupLine($"[fuchsia]{Markup.Escape(result.Path)}[/]");
                        foreach (var (path, ex) in result.ErrorPaths)
                        {
                            var pathInfo = ex.Message.Contains(path, StringComparison.OrdinalIgnoreCase)
                                ? string.Empty
                                : $" (on path: {path})";
                            _console.MarkupLine(
                                $"[purple]-> [/][red]{Markup.Escape(ex.Message)}[/]" +
                                $"[maroon]{Markup.Escape(pathInfo)}[/]");
                        }
                        _console.MarkupLine(string.Empty);
                    }
                }
            }

            _console.Markup(WaitForKeyMessage);
            _ = _console.Input.ReadKey(true);
        }
    }

    /// <summary>
    /// 输出单条文件夹大小结果行
    /// </summary>
    /// <param name="result">文件夹大小结果</param>
    /// <param name="pathLength">路径列宽</param>
    /// <param name="fileCountLength">文件数量列宽</param>
    /// <param name="folderCountLength">文件夹数量列宽</param>
    /// <param name="bytesLength">字节大小列宽</param>
    private void WriteResult(
        FolderSize result,
        int pathLength,
        int fileCountLength,
        int folderCountLength,
        int bytesLength)
    {
        var formattedPath = Markup.Escape(result.Path.PadRight(pathLength));
        var formattedFileCount = result.FileCount.ToString().PadLeft(fileCountLength);
        var formattedFolderCount = result.FolderCount.ToString().PadLeft(folderCountLength);
        var formattedSize = _formatter.Format(result.TotalBytes).PadLeft(_options.SizeStringLength);
        var formattedSizeInBytes = result.TotalBytes.ToString().PadLeft(bytesLength);

        _console.MarkupLine(
            $"[green]{formattedPath}[/]" +
            $"[blue] 包含 [/][aqua]{formattedFileCount}[/][blue] 个文件, [/]" +
            $"[aqua]{formattedFolderCount}[/][blue] 个文件夹, 大小为 [/]" +
            $"[aqua]{formattedSize}[/][white] ( [/][grey]{formattedSizeInBytes}[/][white] 字节)[/]"
        );
    }

    /// <summary>
    /// 双引号字符常量
    /// </summary>
    private const char QuoteChar = '"';

    /// <summary>
    /// 从用户输入中解析文件夹路径并返回标准化的路径列表
    /// </summary>
    /// <param name="input">用户输入的原始字符串</param>
    /// <returns>标准化的文件夹路径列表</returns>
    private List<string> ParsePaths(ReadOnlySpan<char> input)
    {
        var quoteIndices = new List<int>();
        var temp = input;
        int index, consumed = 0;
        while ((index = temp.IndexOf(QuoteChar)) >= 0)
        {
            quoteIndices.Add(consumed + index);
            temp = temp[(index + 1)..];
            consumed += index + 1;
        }
        if (quoteIndices.Count == 0) { return [_pathNormalizer.Normalize(input.ToString())]; }

        var paths = new List<string>();
        for (var i = 0; i < quoteIndices.Count - 1; i += 2)
        {
            var start = quoteIndices[i] + 1;
            var end = quoteIndices[i + 1];
            if (start < end)
            {
                var path = input[start..end].Trim();
                if (path.Length > 0) { paths.Add(_pathNormalizer.Normalize(path.ToString())); }
            }
        }
        return paths;
    }
}
