using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFolderSize.UI.Shared.Interfaces;

/// <summary>
/// 历史存储接口
/// </summary>
public interface IHistoriesStore
{
    /// <summary>
    /// 历史记录的只读列表, 最近添加的路径在前
    /// </summary>
    IReadOnlyList<string> Histories { get; }

    /// <summary>
    /// 添加历史记录并异步保存到文件
    /// </summary>
    /// <param name="histories">历史记录列表</param>
    /// <param name="token">取消令牌</param>
    Task AddHistoriesAsync(IEnumerable<string> histories, CancellationToken token = default);

    /// <summary>
    /// 从历史记录中删除指定的路径列表并异步保存到文件
    /// </summary>
    /// <param name="histories">要删除的历史记录列表</param>
    /// <param name="token">取消令牌</param>
    Task RemoveHistoriesAsync(IEnumerable<string> histories, CancellationToken token = default);

    /// <summary>
    /// 清空历史记录并删除历史记录文件
    /// </summary>
    void Clear();
}
