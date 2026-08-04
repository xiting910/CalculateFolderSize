using CalculateFolderSize.UI.Shared.Interfaces;

namespace CalculateFolderSize.UI.Desktop;

/// <summary>
/// 桌面应用程序的存储访问服务实现
/// </summary>
internal sealed class DesktopStorageAccessService : IStorageAccessService
{
    /// <inheritdoc/>
    public bool IsGranted => true;

    /// <inheritdoc/>
    public void RequestAccess() { }
}
