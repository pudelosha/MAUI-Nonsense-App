using Android.App;
using Android.Content;
using Android.Content.PM; // <-- Needed for LaunchMode
using Android.OS;

namespace MAUI_Nonsense_App.Platforms.Android
{
    // No UI, only redirects to MainActivity when tapped from launcher.
    [Activity(
        Theme = "@android:style/Theme.NoDisplay",
        MainLauncher = true,
        NoHistory = true,
        Exported = true,
        ExcludeFromRecents = true,               // do not show in recents
        LaunchMode = LaunchMode.SingleTask       // prevent duplicate tasks
    )]
    public class LauncherRedirectActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Only open MainActivity if launched from the launcher
            bool fromLauncher =
                Intent?.Action == Intent.ActionMain &&
                (Intent?.Categories?.Contains(Intent.CategoryLauncher) ?? false);

            if (fromLauncher)
            {
                var launch = new Intent(this, typeof(MainActivity));
                launch.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                StartActivity(launch);
            }

            // Do nothing for BOOT_COMPLETED or other system actions
            Finish();
        }
    }
}
