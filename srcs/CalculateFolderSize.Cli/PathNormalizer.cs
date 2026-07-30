using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CalculateFolderSize.Cli;

/// <summary>
/// 路径标准化器
/// </summary>
/// <param name="_options">Cli 配置</param>
internal sealed class PathNormalizer(CliOptions _options) : IPathNormalizer
{
    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) { return path; }

        path = path.Trim().Replace(_options.ReplacedSeparator, _options.DirectorySeparator);
        while (path.Contains(_options.ConsecutiveDirectorySeparators, StringComparison.Ordinal))
        {
            path = path.Replace(_options.ConsecutiveDirectorySeparators, _options.DirectorySeparator.ToString());
        }

        if (path[^1] == _options.DirectorySeparator)
        {
            path = path.TrimEnd(_options.DirectorySeparator);
        }

        if (OperatingSystem.IsWindows())
        {
            if (path.Length > 0 && char.IsLetter(path[0]))
            {
                if (path.Length == 1)
                {
                    path = path + Path.VolumeSeparatorChar + _options.DirectorySeparator;
                }
                else if (path.Length == 2 && path[1] == Path.VolumeSeparatorChar)
                {
                    path += _options.DirectorySeparator;
                }
            }
        }

        return path;
    }
}
