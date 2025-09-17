using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MAUI_Nonsense_App.Platforms.Android.Services.StepCounter
{
    [BroadcastReceiver(Enabled = true, Exported = false, Name = "com.companyname.mauinonsenseapp.MidnightResetReceiver")]
    public class MidnightResetReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var now = DateTime.Now;
            string today = now.Date.ToString("yyyy-MM-dd");
            string yesterday = now.Date.AddDays(-1).ToString("yyyy-MM-dd");

            // Freeze yesterday’s final total into history (even if there was no late tick)
            int yTotal = Preferences.Get("DailySteps", 0);
            var dailyJson = Preferences.Get("StepHistoryDaily", Preferences.Get("StepHistory", "{}"));
            var daily = JsonSerializer.Deserialize<Dictionary<string, int>>(dailyJson) ?? new();
            daily[yesterday] = yTotal;
            Preferences.Set("StepHistoryDaily", JsonSerializer.Serialize(daily));

            // Move midnight baseline to current sensor value, if known
            if (Preferences.ContainsKey("LastSensorReading"))
            {
                int currentSensorValue = Preferences.Get("LastSensorReading", 0);
                Preferences.Set("MidnightStepSensorValue", currentSensorValue);
            }
            else
            {
                Preferences.Remove("MidnightStepSensorValue"); // first tick will set this
            }

            // Start the new day
            Preferences.Set("LastStepDate", today);
            Preferences.Set("ActiveSecondsToday", 0L);
            Preferences.Set("LastStepUnixMs", 0L);
            Preferences.Set("RebootDailyOffset", 0);
            Preferences.Set("DailySteps", 0);

            // Ensure entries for the new day
            var hourlyJson = Preferences.Get("StepHistoryHourly", "{}");
            var hourly = JsonSerializer.Deserialize<Dictionary<string, int[]>>(hourlyJson) ?? new();
            if (!hourly.ContainsKey(today)) hourly[today] = new int[24];
            Preferences.Set("StepHistoryHourly", JsonSerializer.Serialize(hourly));

            if (!daily.ContainsKey(today)) daily[today] = 0;
            Preferences.Set("StepHistoryDaily", JsonSerializer.Serialize(daily));

            // NEW: Immediately refresh the persistent notification to show 0
            UpdateNotificationToZero(context);
        }

        // --- helpers ---

        private void UpdateNotificationToZero(Context context)
        {
            // Uses the same channel ("step_counter_channel") and ID (101) as the foreground service
            var builder = new NotificationCompat.Builder(context, "step_counter_channel")
                .SetContentTitle("Step Counter")
                .SetContentText("Today's steps: 0")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetOngoing(true)
                .SetContentIntent(BuildLaunchIntent(context));

            NotificationManagerCompat.From(context).Notify(101, builder.Build());
        }

        private PendingIntent BuildLaunchIntent(Context context)
        {
            // Keep tap-to-open StepCounterPage behavior consistent with the service notification
            var intent = new Intent(context, typeof(MAUI_Nonsense_App.MainActivity))
                .SetAction(Intent.ActionMain)
                .AddCategory(Intent.CategoryLauncher)
                .SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

            intent.PutExtra("navigateTo", "StepCounter");

            var flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
            return PendingIntent.GetActivity(context, 0, intent, flags);
        }
    }
}
