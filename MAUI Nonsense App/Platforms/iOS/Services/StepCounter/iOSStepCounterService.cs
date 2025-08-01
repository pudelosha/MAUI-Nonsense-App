using CoreMotion;
using Foundation;
using MAUI_Nonsense_App.Services;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace MAUI_Nonsense_App.Platforms.iOS.Services.StepCounter
{
    public class iOSStepCounterService : IStepCounterService
    {
        private readonly CMPedometer _pedometer = new();
        private int _currentSteps = 0;

        public int TotalSteps => _currentSteps;
        public int RawSensorValue => _currentSteps;

        public int Last24HoursSteps
        {
            get
            {
                int midnight = Preferences.Get("MidnightStepSensorValue", 0);
                return Math.Max(0, TotalSteps - midnight);
            }
        }

        public Dictionary<string, int> StepHistory => GetStepHistory();

        public Task StartAsync() => Task.CompletedTask;
        public Task StopAsync() => Task.CompletedTask;

        public void ResetAll()
        {
            Preferences.Remove("MidnightStepSensorValue");
            Preferences.Remove("LastStepDate");
            Preferences.Set("StepHistory", "{}");
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

            SaveDailySnapshotIfNeeded(_currentSteps);
            return _currentSteps;
        }

        // ✅ Implementing the missing interface method
        public void SaveDailySnapshotIfNeeded(int currentSensorValue)
        {
            UpdateDailySnapshot(currentSensorValue);
        }

        private void UpdateDailySnapshot(int currentValue)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string lastSavedDate = Preferences.Get("LastStepDate", "");

            var history = GetStepHistory();

            // Save yesterday’s value if missed
            if (!string.IsNullOrEmpty(lastSavedDate) && lastSavedDate != today)
            {
                if (!history.ContainsKey(lastSavedDate))
                {
                    int yesterdaySteps = Preferences.Get("MidnightStepSensorValue", 0);
                    history[lastSavedDate] = yesterdaySteps;
                }
            }

            Preferences.Set("MidnightStepSensorValue", currentValue);
            Preferences.Set("LastStepDate", today);

            // Update today’s count
            history[today] = currentValue;

            Preferences.Set("StepHistory", JsonSerializer.Serialize(history));
        }

        private Dictionary<string, int> GetStepHistory()
        {
            var json = Preferences.Get("StepHistory", "{}");
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                   ?? new Dictionary<string, int>();
        }
    }
}
