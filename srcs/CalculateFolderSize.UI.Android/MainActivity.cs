using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace CalculateFolderSize.UI.Android;

/// <summary>
/// 主活动类
/// </summary>
[Activity(
    Label = nameof(CalculateFolderSize),
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode
)]
public class MainActivity : AvaloniaMainActivity;
