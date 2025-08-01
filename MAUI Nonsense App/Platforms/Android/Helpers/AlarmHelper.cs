using Android.App;
using Android.Content;
using Java.Util;

namespace MAUI_Nonsense_App.Platforms.Android.Helpers
{
    public static class AlarmHelper
    {
        public static void ScheduleNextMidnightSnapshot(Context context)
        {
            var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);

            Intent intent = new(context, typeof(BroadcastReceiver));
            intent.SetAction("MAUI_Nonsense_App.ACTION_SNAPSHOT");

            var pendingIntent = PendingIntent.GetBroadcast(
                context,
                0,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
            );

            // Calculate next local midnight
            var calendar = Calendar.Instance;
            calendar.TimeInMillis = Java.Lang.JavaSystem.CurrentTimeMillis();
            calendar.Set(CalendarField.HourOfDay, 0);
            calendar.Set(CalendarField.Minute, 0);
            calendar.Set(CalendarField.Second, 0);
            calendar.Set(CalendarField.Millisecond, 0);
            calendar.Add(CalendarField.DayOfYear, 1); // next midnight

            long triggerAtMillis = calendar.TimeInMillis;

            alarmManager?.SetExactAndAllowWhileIdle(
                AlarmType.RtcWakeup,
                triggerAtMillis,
                pendingIntent
            );
        }
    }
}
