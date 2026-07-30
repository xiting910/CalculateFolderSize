using System.Diagnostics.CodeAnalysis;

namespace CalculateFolderSize.Cli;

/// <summary>
/// 路径标准化器接口
/// </summary>
public interface IPathNormalizer
{
    /// <summary>
    /// 对指定的路径进行标准化处理
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>标准化后的路径</returns>
    [return: NotNullIfNotNull(nameof(path))]
    string? Normalize(string? path);
}
