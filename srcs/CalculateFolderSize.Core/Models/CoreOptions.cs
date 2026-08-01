using Microsoft.Extensions.Configuration;
using System;

namespace CalculateFolderSize.Core.Models;

/// <summary>
/// <see cref="Core"/> 层的配置选项
/// </summary>
/// <param name="DecimalPlaces">小数位数</param>
/// <param name="MaxDegreeOfParallelism">最大并行度</param>
/// <param name="CaptureChildren">是否捕获子项</param>
/// <param name="PathComparer">路径比较器</param>
public sealed record CoreOptions(
    int DecimalPlaces,
    int MaxDegreeOfParallelism,
    bool CaptureChildren,
    StringComparer PathComparer)
{
    /// <summary>
    /// 使用 <see cref="IConfiguration"/> 的构造函数
    /// </summary>
    /// <param name="configuration">要使用的 <see cref="IConfiguration"/> 实例</param>
    public CoreOptions(IConfiguration configuration) : this(Create(configuration)) { }

    /// <summary>
    /// 创建 <see cref="CoreOptions"/> 实例
    /// </summary>
    /// <param name="configuration">要使用的 <see cref="IConfiguration"/> 实例</param>
    /// <returns>创建的 <see cref="CoreOptions"/> 实例</returns>
    private static CoreOptions Create(IConfiguration configuration)
    {
        var section = configuration.GetSection(nameof(Core));

        if (!int.TryParse(section[nameof(DecimalPlaces)], out var decimalPlaces) || decimalPlaces < 0)
        {
            decimalPlaces = 2;
        }

        if (!int.TryParse(section[nameof(MaxDegreeOfParallelism)], out var maxDegree) || maxDegree <= 0)
        {
            maxDegree = Environment.ProcessorCount * 2;
        }

        if (!bool.TryParse(section[nameof(CaptureChildren)], out var captureChildren))
        {
            captureChildren = false;
        }

        var pathComparer = StringComparer.GetPathComparer(section[nameof(PathComparer)]);
        return new(decimalPlaces, maxDegree, captureChildren, pathComparer);
    }
}
