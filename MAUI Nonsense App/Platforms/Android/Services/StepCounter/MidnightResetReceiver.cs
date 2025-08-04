using Android.Content;
using Microsoft.Maui.Storage;
using System;

namespace MAUI_Nonsense_App.Platforms.Android.Services.StepCounter
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    public class MidnightResetReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            string today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            int currentSensorValue = Preferences.Get("LastSensorReading", 0);

            Preferences.Set("MidnightStepSensorValue", currentSensorValue);
            Preferences.Set("LastStepDate", today);

            var json = Preferences.Get("StepHistory", "{}");
            var history = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                          ?? new Dictionary<string, int>();

            if (!history.ContainsKey(today) || history[today] <= 0)
                history[today] = 0;

            Preferences.Set("StepHistory", System.Text.Json.JsonSerializer.Serialize(history));
        }
    }
}
