using Android.App;
using Android.Content;
using Android.OS;

namespace MAUI_Nonsense_App.Platforms.Android
{
    [Activity(Theme = "@android:style/Theme.NoDisplay",
              MainLauncher = true,
              NoHistory = true,
              Exported = true)]
    public class LauncherRedirectActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // If the system didn't start this via reboot, go to MainActivity
            if (Intent?.Action != Intent.ActionBootCompleted)
            {
                var intent = new Intent(this, typeof(MainActivity));
                intent.AddFlags(ActivityFlags.NewTask);
                StartActivity(intent);
            }

            Finish(); // exit immediately
        }
    }
}
