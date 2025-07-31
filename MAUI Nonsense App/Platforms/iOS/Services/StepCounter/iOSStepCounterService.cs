using CoreMotion;
using Foundation;
using MAUI_Nonsense_App.Services;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace MAUI_Nonsense_App.Platforms.iOS.Services.StepCounter
{
    public class iOSStepCounterService : IStepCounterService
    {
        private readonly CMPedometer _pedometer = new CMPedometer();
        private int _currentSteps = 0;

        public int TotalSteps => _currentSteps;

        public int Last24HoursSteps
        {
            get
            {
                int atMidnight = Preferences.Get("MidnightStepSensorValue", 0);
                return Math.Max(0, TotalSteps - atMidnight);
            }
        }

        public Dictionary<string, int> StepHistory => GetStepHistory();

        public int RawSensorValue => _currentSteps; // For cross-platform debug

        public Task StartAsync() => Task.CompletedTask;

        public Task StopAsync() => Task.CompletedTask;

        public void ResetAll()
        {
            Preferences.Remove("MidnightStepSensorValue");
            Preferences.Remove("LastStepDate");
            Preferences.Set("StepHistory", "{}");
        }

        public void SaveDailySnapshotIfNeeded(int currentSensorValue)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var lastSavedDate = Preferences.Get("LastStepDate", "");

            if (today != lastSavedDate)
            {
                var history = GetStepHistory();
                history[today] = currentSensorValue;

                Preferences.Set("StepHistory", JsonSerializer.Serialize(history));
                Preferences.Set("LastStepDate", today);
                Preferences.Set("MidnightStepSensorValue", currentSensorValue);
            }
        }

        public async Task<int> FetchCurrentStepsAsync()
        {
            if (!CMPedometer.IsStepCountingAvailable)
                return 0;

            var now = NSDate.Now;

            var calendar = NSCalendar.CurrentCalendar;
            var components = calendar.Components(NSCalendarUnit.Year | NSCalendarUnit.Month | NSCalendarUnit.Day, now);
            var midnight = calendar.DateFromComponents(components);

            var tcs = new TaskCompletionSource<int>();

            _pedometer.QueryPedometerData(midnight, now, (data, error) =>
            {
                if (error != null || data?.NumberOfSteps == null)
                {
                    tcs.SetResult(0);
                }
                else
                {
                    tcs.SetResult(data.NumberOfSteps.Int32Value);
                }
            });

            _currentSteps = await tcs.Task;

            SaveDailySnapshotIfNeeded(_currentSteps); // Update today's snapshot
            return _currentSteps;
        }

        private Dictionary<string, int> GetStepHistory()
        {
            var json = Preferences.Get("StepHistory", "{}");
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                   ?? new Dictionary<string, int>();
        }
    }
}
