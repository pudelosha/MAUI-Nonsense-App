using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace MAUI_Nonsense_App.Platforms.Android.Services.MovementAlarm
{
    [Service(ForegroundServiceType = ForegroundService.TypeDataSync)]
    public class MovementAlarmForegroundService : Service
    {
        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            if (intent == null)
            {
                // Service restarted by the system without an intent. Use defaults or do nothing.
                System.Diagnostics.Debug.WriteLine("OnStartCommand: intent is null. Using defaults.");
                return StartCommandResult.Sticky;
            }

            int sensitivity = intent.GetIntExtra("sensitivity", 2);
            int armingDelay = intent.GetIntExtra("armingDelay", 10);

            var notification = BuildNotification();
            StartForeground(1001, notification);

            return StartCommandResult.Sticky;
        }

        private Notification BuildNotification()
        {
            var builder = new Notification.Builder(this, "movement_alarm_channel")
                .SetContentTitle("Movement Alarm Armed")
                .SetContentText("Monitoring movement…")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetOngoing(true);

            return builder.Build();
        }

        public override IBinder? OnBind(Intent intent) => null;
    }
}
