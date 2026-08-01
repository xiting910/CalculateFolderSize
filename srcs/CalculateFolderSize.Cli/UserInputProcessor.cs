using System;
using System.Collections.Generic;

namespace CalculateFolderSize.Cli;

/// <summary>
/// 用户输入处理器
/// </summary>
/// <param name="_pathNormalizer">路径标准化器</param>
internal sealed class UserInputProcessor(IPathNormalizer _pathNormalizer) : IUserInputProcessor
{
    /// <summary>
    /// 双引号字符常量
    /// </summary>
    private const char QuoteChar = '"';

    /// <summary>
    /// 记录输入字符串中双引号的位置
    /// </summary>
    private readonly List<int> _quoteIndices = [];

    /// <inheritdoc/>
    public void ParsePaths(ReadOnlySpan<char> input, List<string> paths)
    {
        paths.Clear();
        _quoteIndices.Clear();

        var temp = input;
        int index, consumed = 0;
        while ((index = temp.IndexOf(QuoteChar)) >= 0)
        {
            _quoteIndices.Add(consumed + index);
            temp = temp[(index + 1)..];
            consumed += index + 1;
        }

        if (_quoteIndices.Count == 0)
        {
            paths.Add(_pathNormalizer.Normalize(input.ToString()));
            return;
        }

        for (var i = 0; i < _quoteIndices.Count - 1; i += 2)
        {
            var start = _quoteIndices[i] + 1;
            var end = _quoteIndices[i + 1];
            if (start < end)
            {
                var path = input[start..end].Trim();
                if (path.Length > 0) { paths.Add(_pathNormalizer.Normalize(path.ToString())); }
            }
        }
    }
}
