// Platforms/Android/MainActivity.cs
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
using Microsoft.Maui.ApplicationModel;   // MainThread
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;           // Shell / NavigationPage
using System;
using System.Threading.Tasks;

namespace MAUI_Nonsense_App
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = false, LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                              ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : MauiAppCompatActivity
    {
        private bool _serviceStarted = false;

        // Deep-link state
        private string? _pendingNavigateTo;
        private bool _handledPending;

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

            // Capture deep-link from notification (don't navigate yet)
            _pendingNavigateTo = GetTargetFromIntent(Intent);
            _handledPending = false;
        }

        protected override async void OnResume()
        {
            base.OnResume();

            if (_serviceStarted == false)
            {
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

            // Now it's safe to navigate
            await ProcessPendingNavigationAsync();
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

        // -------- handle notification taps while app is already running --------
        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            _pendingNavigateTo = GetTargetFromIntent(intent);
            _handledPending = false;
        }

        private static string? GetTargetFromIntent(Intent? intent)
        {
            if (intent == null) return null;
            var target = intent.GetStringExtra("navigateTo");
            return string.IsNullOrWhiteSpace(target) ? null : target;
        }

        private Task ProcessPendingNavigationAsync()
        {
            if (_handledPending || string.IsNullOrWhiteSpace(_pendingNavigateTo))
                return Task.CompletedTask;

            var target = _pendingNavigateTo;
            _handledPending = true;
            _pendingNavigateTo = null;

            if (!target.Equals("StepCounter", StringComparison.OrdinalIgnoreCase) &&
                !target.Equals("StepCounterPage", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // tiny delay to ensure Navigation host is attached
                await Task.Delay(200);

                var mainPage = App.Current?.MainPage;
                if (mainPage == null) return;

                try
                {
                    if (mainPage is Shell shell)
                    {
                        // Ensure this route is registered in AppShell:
                        // Routing.RegisterRoute("stepcounter", typeof(Pages.Activity.StepCounterPage));
                        await shell.GoToAsync("stepcounter");
                        return;
                    }

                    if (mainPage is NavigationPage nav)
                    {
                        var svc = MauiApplication.Current.Services
                            .GetService(typeof(IStepCounterService)) as IStepCounterService;

                        await nav.PushAsync(new Pages.Activity.StepCounterPage(svc));
                    }
                }
                catch
                {
                    // ignore navigation exceptions if app is closing, etc.
                }
            });

            return Task.CompletedTask;
        }
    }
}
