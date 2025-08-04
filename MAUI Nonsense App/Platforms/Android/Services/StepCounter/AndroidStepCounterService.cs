using Android.App;
using Android.Content;
using Android.OS;
using MAUI_Nonsense_App.Services;
using Microsoft.Maui.Storage;

using AApp = Android.App.Application;

namespace MAUI_Nonsense_App.Platforms.Android.Services.StepCounter
{
    public class AndroidStepCounterService : IStepCounterService
    {
        private readonly Context _context;

        public static AndroidStepCounterService? Instance { get; private set; }

        public int TotalSteps => Preferences.Get("AccumulatedSteps", 0);
        public int Last24HoursSteps => Preferences.Get("DailySteps", 0);
        public Dictionary<string, int> StepHistory => GetStepHistory();

        public event EventHandler? StepsUpdated;

        public AndroidStepCounterService()
        {
            _context = AApp.Context;
            Instance = this;
        }

        public Task StartAsync()
        {
            StartForegroundService();
            ScheduleMidnightReset();
            RaiseStepsUpdated();
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            StopForegroundService();
            return Task.CompletedTask;
        }

        public void ResetAll()
        {
            Preferences.Set("AccumulatedSteps", 0);
            Preferences.Set("DailySteps", 0);
            Preferences.Remove("FirstEverStepSensorValue");
            Preferences.Remove("MidnightStepSensorValue");
            Preferences.Set("StepHistory", "{}");
            Preferences.Set("LastStepDate", DateTime.UtcNow.ToString("yyyy-MM-dd"));
        }

        public void RaiseStepsUpdated()
        {
            Console.WriteLine("[AndroidStepCounterService] RaiseStepsUpdated called");
            StepsUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void ScheduleMidnightReset()
        {
            var intent = new Intent(_context, typeof(MidnightResetReceiver));
            var pendingIntent = PendingIntent.GetBroadcast(_context, 0, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

            var alarmManager = (AlarmManager)_context.GetSystemService(Context.AlarmService);
            var calendar = Java.Util.Calendar.Instance;
            calendar.TimeInMillis = Java.Lang.JavaSystem.CurrentTimeMillis();
            calendar.Set(Java.Util.CalendarField.HourOfDay, 0);
            calendar.Set(Java.Util.CalendarField.Minute, 0);
            calendar.Set(Java.Util.CalendarField.Second, 0);
            calendar.Add(Java.Util.CalendarField.DayOfYear, 1);

            alarmManager.SetInexactRepeating(
                AlarmType.RtcWakeup,
                calendar.TimeInMillis,
                AlarmManager.IntervalDay,
                pendingIntent);
        }

        private void StartForegroundService()
        {
            var intent = new Intent(_context, typeof(StepCounterForegroundService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                _context.StartForegroundService(intent);
            else
                _context.StartService(intent);
        }

        private void StopForegroundService()
        {
            var intent = new Intent(_context, typeof(StepCounterForegroundService));
            _context.StopService(intent);
        }

        private Dictionary<string, int> GetStepHistory()
        {
            var json = Preferences.Get("StepHistory", "{}");
            var history = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                          ?? new Dictionary<string, int>();
            return history;
        }
    }
}
