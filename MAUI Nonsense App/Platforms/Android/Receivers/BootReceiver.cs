using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Maui.Storage;
using System.Threading;
using MAUI_Nonsense_App.Platforms.Android.Services.StepCounter;
using Android.Util;

namespace MAUI_Nonsense_App.Platforms.Android.Receivers
{
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    [BroadcastReceiver(Enabled = true, Exported = true, Name = "com.companyname.mauinonsenseapp.BootReceiver")]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent?.Action == Intent.ActionBootCompleted)
            {
                // Reset step values or any other boot-time data
                Preferences.Set("BootBaseStepValue", 0);

                // Optional: respect toggle if user disabled boot-start
                bool startOnBoot = Preferences.Get("StartServiceOnBoot", true);
                if (!startOnBoot) return;

                // Delay to avoid "unsafe to start foreground service" error
                new Thread(() =>
                {
                    Thread.Sleep(10000); // 10 seconds delay
                    Log.Debug("BootReceiver", "Attempting to start AndroidStepCounterService after delay");
                    _ = new AndroidStepCounterService().StartAsync();
                }).Start();
            }
        }
    }
}
