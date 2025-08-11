using Android.Content;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;

namespace MAUI_Nonsense_App.Platforms.Android.Services.StepCounter
{
    [BroadcastReceiver(Enabled = true, Exported = false, Name = "com.companyname.mauinonsenseapp.MidnightResetReceiver")]
    public class MidnightResetReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            string today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            int currentSensorValue = Preferences.Get("LastSensorReading", 0);

            // Set the new baseline at midnight (relative to whatever the sensor is reporting now)
            Preferences.Set("MidnightStepSensorValue", currentSensorValue);
            Preferences.Set("LastStepDate", today);

            // Ensure history entry exists for today (starts at 0 after midnight)
            var json = Preferences.Get("StepHistory", "{}");
            var history = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                          ?? new Dictionary<string, int>();

            if (!history.ContainsKey(today) || history[today] < 0)
                history[today] = 0;

            Preferences.Set("StepHistory", System.Text.Json.JsonSerializer.Serialize(history));

            // IMPORTANT: clear any carry-over from reboots
            Preferences.Set("RebootDailyOffset", 0);
            // DailySteps will be recomputed on the next sensor tick from the midnight baseline
            Preferences.Set("DailySteps", 0);
        }
    }
}
