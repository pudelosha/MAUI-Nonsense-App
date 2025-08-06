using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using MAUI_Nonsense_App.Services;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace MAUI_Nonsense_App
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = false, LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                              ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Permissions
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q &&
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.ActivityRecognition) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.ActivityRecognition }, 0);
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S &&
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.ForegroundServiceConnectedDevice) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.ForegroundServiceConnectedDevice }, 1);
            }

            RegisterAllNotificationChannels();
        }

        private bool _serviceStarted = false;

        protected override async void OnResume()
        {
            base.OnResume();

            if (_serviceStarted) return;
            _serviceStarted = true;

            await Task.Delay(500); // ensure UI is visible

            if (!AreNotificationsEnabled())
            {
                bool goToSettings = await App.Current.MainPage.DisplayAlert(
                    "Enable Notifications",
                    "Notifications are disabled. Please enable them in system settings to receive step updates.",
                    "Go to Settings",
                    "Later");

                if (goToSettings)
                    OpenAppNotificationSettings();
            }

            var serviceProvider = MauiApplication.Current.Services;
            var service = serviceProvider.GetService(typeof(IStepCounterService)) as IStepCounterService;
            if (service != null)
            {
                await service.StartAsync();
            }
        }

        private void RegisterAllNotificationChannels()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var movementChannel = new NotificationChannel(
                    "movement_alarm_channel",
                    "Step Movement Alerts",
                    NotificationImportance.Default)
                {
                    Description = "Step detection notifications"
                };

                var stepCounterChannel = new NotificationChannel(
                    "step_counter_channel",
                    "Step Counter Background Service",
                    NotificationImportance.Low)
                {
                    Description = "Persistent step tracking service"
                };

                var manager = (NotificationManager)GetSystemService(NotificationService);
                manager.CreateNotificationChannel(movementChannel);
                manager.CreateNotificationChannel(stepCounterChannel);
            }
        }

        private bool AreNotificationsEnabled()
        {
            var manager = NotificationManagerCompat.From(this);
            return manager.AreNotificationsEnabled();
        }

        private void OpenAppNotificationSettings()
        {
            Intent intent = new Intent();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                intent.SetAction(Settings.ActionAppNotificationSettings);
                intent.PutExtra(Settings.ExtraAppPackage, PackageName);
            }
            else
            {
                intent.SetAction(Settings.ActionApplicationDetailsSettings);
                intent.SetData(Android.Net.Uri.Parse("package:" + PackageName));
            }

            intent.AddFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }
    }
}
