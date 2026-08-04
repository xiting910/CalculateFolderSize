namespace CalculateFolderSize.UI.Shared.Interfaces;

/// <summary>
/// 存储访问服务接口
/// </summary>
public interface IStorageAccessService
{
    /// <summary>
    /// 是否已授予访问权限
    /// </summary>
    bool IsGranted { get; }

    /// <summary>
    /// 请求访问权限
    /// </summary>
    void RequestAccess();
}
