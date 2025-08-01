using Android.App;
using Android.Content;
using MAUI_Nonsense_App.Platforms.Android.Helpers;
using System.Text.Json;

namespace MAUI_Nonsense_App.Platforms.Android.Services.StepCounter
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { "MAUI_Nonsense_App.ACTION_SNAPSHOT" })]
    public class SnapshotReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            int rawSensorValue = Preferences.Get("LastSensorValue", 0);
            int midnightValue = Preferences.Get("MidnightStepSensorValue", rawSensorValue);
            int stepsToday = Math.Max(0, rawSensorValue - midnightValue);

            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var historyJson = Preferences.Get("StepHistory", "{}");
            var history = JsonSerializer.Deserialize<Dictionary<string, int>>(historyJson)
                          ?? new Dictionary<string, int>();

            history[today] = stepsToday;
            Preferences.Set("StepHistory", JsonSerializer.Serialize(history));

            // Schedule next midnight snapshot
            AlarmHelper.ScheduleNextMidnightSnapshot(context);
        }
    }
}
