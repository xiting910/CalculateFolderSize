using Android.App;
using Android.Content;
using Android.Net;
using Android.Provider;
using CalculateFolderSize.UI.Shared.Interfaces;

namespace CalculateFolderSize.UI.Android;

/// <summary>
/// 安卓全部文件访问权限服务, 检查并引导授予 MANAGE_EXTERNAL_STORAGE 权限
/// </summary>
internal sealed class StorageAccessService : IStorageAccessService
{
    /// <inheritdoc/>
    public bool IsGranted => AndroidEnvironment.IsExternalStorageManager;

    /// <inheritdoc/>
    public void RequestAccess()
    {
        var intent = new Intent(
            Settings.ActionManageAppAllFilesAccessPermission,
            Uri.Parse($"package:{Application.Context.PackageName}")
        );

        Application.Context.StartActivity(intent.AddFlags(ActivityFlags.NewTask));
    }
}
