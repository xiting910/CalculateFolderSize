using System;
using System.Collections.Generic;

namespace CalculateFolderSize.Cli;

/// <summary>
/// 用户输入处理器接口
/// </summary>
public interface IUserInputProcessor
{
    /// <summary>
    /// 解析用户输入的路径
    /// </summary>
    /// <param name="input">用户输入</param>
    /// <param name="paths">解析后的路径列表</param>
    void ParsePaths(ReadOnlySpan<char> input, List<string> paths);
}
