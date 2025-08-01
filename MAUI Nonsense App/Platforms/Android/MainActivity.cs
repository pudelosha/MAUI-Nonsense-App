using Android;
using Android.App;
using Android.Content;
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

            RequestPermissionsIfNeeded();

            RegisterMovementAlarmNotificationChannel();

            // Start step counter service
            StartStepCounterForegroundService();
        }

        private void RequestPermissionsIfNeeded()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q) // Android 10+
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ActivityRecognition) != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.ActivityRecognition }, 0);
                }
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 13+
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ScheduleExactAlarm) != Permission.Granted)
                {
                    // Not requestable directly; app must be user-approved via system settings
                    // Optional: Prompt user to enable it manually
                }
            }
        }

        private void StartStepCounterForegroundService()
        {
            var stepServiceIntent = new Intent(this, typeof(Platforms.Android.Services.StepCounter.StepCounterForegroundService));

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                StartForegroundService(stepServiceIntent);
            else
                StartService(stepServiceIntent);
        }

        private void RegisterMovementAlarmNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
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
