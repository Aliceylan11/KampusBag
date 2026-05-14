using Android.App;
using Android.Content.PM;
using Android.OS;

namespace KampusBag.MobileUI
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Android 13+ için bildirim izni iste
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.RequestPermissions(
                    new string[] { Android.Manifest.Permission.PostNotifications }, 0);
            }
        }
    }
}
