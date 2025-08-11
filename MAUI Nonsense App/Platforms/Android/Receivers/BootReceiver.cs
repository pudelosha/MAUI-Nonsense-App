using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Microsoft.Maui.Storage;
using System;

namespace MAUI_Nonsense_App.Platforms.Android.Receivers
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = "com.companyname.mauinonsenseapp.BootReceiver")]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent == null || intent.Action != Intent.ActionBootCompleted)
                return;

            try
            {
                bool startOnBoot = Preferences.Get("StartServiceOnBoot", true);
                if (!startOnBoot)
                {
                    Log.Info("BootReceiver", "StartServiceOnBoot is disabled. Aborting.");
                    return;
                }

                string today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
                string lastDate = Preferences.Get("LastStepDate", today);
                int lastDaily = Preferences.Get("DailySteps", 0);

                // If we reboot during the same calendar day, carry today's steps forward
                if (lastDate == today)
                {
                    Preferences.Set("RebootDailyOffset", Math.Max(0, lastDaily));
                }
                else
                {
                    // New day already: no carry
                    Preferences.Set("RebootDailyOffset", 0);
                    Preferences.Set("LastStepDate", today);
                    // Midnight baseline will be rebuilt on first tick from the (now fresh) counter
                }

                // The hardware counter restarts from 0 after reboot. Make that explicit.
                Preferences.Set("MidnightStepSensorValue", 0);
                Preferences.Set("LastSensorReading", 0);

                Log.Info("BootReceiver", "Boot completed. Starting StepCounterForegroundService...");

                var serviceIntent = new Intent(context, typeof(Services.StepCounter.StepCounterForegroundService));
                serviceIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ExcludeFromRecents);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    context.StartForegroundService(serviceIntent);
                else
                    context.StartService(serviceIntent);
            }
            catch (Exception ex)
            {
                Log.Error("BootReceiver", $"BootReceiver error: {ex}");
            }
        }
    }
}
