using Android.Content;
using Android.App;

namespace MAUI_Nonsense_App.Platforms.Android.Services.StepCounter
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    public class BootCompletedReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == Intent.ActionBootCompleted)
            {
                // Reschedule the midnight alarm after reboot
                var alarmIntent = new Intent(context, typeof(MidnightResetReceiver));
                var pendingIntent = PendingIntent.GetBroadcast(context, 0, alarmIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

                var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);
                var calendar = Java.Util.Calendar.Instance;
                calendar.TimeInMillis = Java.Lang.JavaSystem.CurrentTimeMillis();
                calendar.Set(Java.Util.CalendarField.HourOfDay, 0);
                calendar.Set(Java.Util.CalendarField.Minute, 0);
                calendar.Set(Java.Util.CalendarField.Second, 0);
                calendar.Add(Java.Util.CalendarField.DayOfYear, 1); // Next midnight

                alarmManager.SetInexactRepeating(
                    AlarmType.RtcWakeup,
                    calendar.TimeInMillis,
                    AlarmManager.IntervalDay,
                    pendingIntent);
            }
        }
    }
}
