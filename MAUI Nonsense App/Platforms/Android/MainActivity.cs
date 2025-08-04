using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using MAUI_Nonsense_App.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui;

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

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ActivityRecognition) != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.ActivityRecognition }, 0);
                }
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ForegroundServiceConnectedDevice) != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.ForegroundServiceConnectedDevice }, 1);
                }
            }

            RegisterMovementAlarmNotificationChannel();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(1000); // slight delay to let MAUI initialize
                var serviceProvider = MauiApplication.Current.Services;
                var service = serviceProvider.GetService(typeof(IStepCounterService)) as IStepCounterService;
                if (service != null)
                {
                    await service.StartAsync();
                }
            });
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
