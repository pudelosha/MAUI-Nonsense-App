using Android;
using Android.App;

using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace MAUI_Nonsense_App
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                              ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
                              ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);











            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q) // Android 10+
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ActivityRecognition) != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ActivityRecognition }, 0);
                }
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) // Android 12+
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ForegroundServiceConnectedDevice) != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ForegroundServiceConnectedDevice }, 1);

                }
            }


            RegisterMovementAlarmNotificationChannel();







        }

        private void RegisterMovementAlarmNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) // Android 8.0+
            {
                var channel = new NotificationChannel(
                    "movement_alarm_channel",
                    "Movement Alarm",
                    NotificationImportance.Default);

                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }
    }
}