using Android.Content;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace MAUI_Nonsense_App.Platforms.Android.Services.StepCounter
{
    [BroadcastReceiver(Enabled = true, Exported = false, Name = "com.companyname.mauinonsenseapp.MidnightResetReceiver")]
    public class MidnightResetReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            string today = DateTime.Now.Date.ToString("yyyy-MM-dd");
            int currentSensorValue = Preferences.Get("LastSensorReading", 0);

            // Baseline moves to current sensor count at local midnight
            Preferences.Set("MidnightStepSensorValue", currentSensorValue);
            Preferences.Set("LastStepDate", today);
            Preferences.Set("ActiveSecondsToday", 0L);
            Preferences.Set("LastStepUnixMs", 0L);
            Preferences.Set("RebootDailyOffset", 0);
            Preferences.Set("DailySteps", 0);

            // Ensure daily & hourly entries for new day
            var dailyJson = Preferences.Get("StepHistoryDaily", Preferences.Get("StepHistory", "{}"));
            var daily = JsonSerializer.Deserialize<Dictionary<string, int>>(dailyJson) ?? new();
            if (!daily.ContainsKey(today)) daily[today] = 0;
            Preferences.Set("StepHistoryDaily", JsonSerializer.Serialize(daily));

            var hourlyJson = Preferences.Get("StepHistoryHourly", "{}");
            var hourly = JsonSerializer.Deserialize<Dictionary<string, int[]>>(hourlyJson) ?? new();
            if (!hourly.ContainsKey(today)) hourly[today] = new int[24];
            Preferences.Set("StepHistoryHourly", JsonSerializer.Serialize(hourly));
        }
    }
}
