using CalculateFolderSize.Core.Models;
using CalculateFolderSize.UI.Shared.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFolderSize.UI.Shared.Interfaces;

/// <summary>
/// 设置存储接口
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// 异步更新 <see cref="CoreOptions"/> 实例
    /// </summary>
    /// <remarks>
    /// 该方法需要重启应用程序才能生效, 因为 <see cref="CoreOptions"/> 的实例是只读的
    /// </remarks>
    /// <param name="updateFunc">用于更新 <see cref="CoreOptions"/> 实例的函数</param>
    /// <param name="token">取消令牌</param>
    Task UpdateCoreOptionsAsync(Func<CoreOptions, CoreOptions> updateFunc, CancellationToken token = default);

    /// <summary>
    /// 异步更新 <see cref="UIOptions"/> 实例
    /// </summary>
    /// <param name="updateFunc">用于更新 <see cref="UIOptions"/> 实例的函数</param>
    /// <param name="token">取消令牌</param>
    Task UpdateUIOptionsAsync(Func<UIOptions, UIOptions> updateFunc, CancellationToken token = default);
}
