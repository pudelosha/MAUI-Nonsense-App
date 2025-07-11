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
        private readonly Handler _handler;
        private const int PollIntervalMs = 2000;

        public int TotalSteps => Preferences.Get("AccumulatedSteps", 0);
        public int Last24HoursSteps => Preferences.Get("DailySteps", 0);
        public Dictionary<string, int> StepHistory => GetStepHistory();

        public event EventHandler? StepsUpdated;

        public AndroidStepCounterService()
        {
            _context = AApp.Context;
            _handler = new Handler();
        }

        public Task StartAsync()
        {
            StartForegroundService();
            StartPolling();
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            StopForegroundService();
            _handler.RemoveCallbacksAndMessages(null);
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

        private void StartPolling()
        {
            _handler.PostDelayed(() =>
            {
                StepsUpdated?.Invoke(this, EventArgs.Empty);
                StartPolling();
            }, PollIntervalMs);
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
