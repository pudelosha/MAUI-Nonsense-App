using CoreMotion;
using Foundation;
using MAUI_Nonsense_App.Services;


namespace MAUI_Nonsense_App.Platforms.iOS.Services.StepCounter
{
    public class iOSStepCounterService : IStepCounterService
    {
        private readonly CMPedometer _pedometer = new CMPedometer();

        public int TotalSteps => Preferences.Get("AccumulatedSteps", 0);
        public int Last24HoursSteps => Preferences.Get("DailySteps", 0);
        public Dictionary<string, int> StepHistory => GetStepHistory();

        public event EventHandler? StepsUpdated;

        public async Task StartAsync()
        {
            if (!CMPedometer.IsStepCountingAvailable)
                return;

            var now = NSDate.Now;

            var calendar = NSCalendar.CurrentCalendar;
            var components = calendar.Components(NSCalendarUnit.Year | NSCalendarUnit.Month | NSCalendarUnit.Day, now);
            var midnight = calendar.DateFromComponents(components);

            string today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            string storedDate = Preferences.Get("StepCounterDate", today);

            if (storedDate != today)
            {
                Preferences.Set("StepCounterDate", today);
                Preferences.Set("DailySteps", 0);
                UpdateStepHistory(storedDate, Preferences.Get("DailySteps", 0));
            }

            _pedometer.StartPedometerUpdates(midnight, (data, error) =>
            {
                if (data != null)
                {
                    int dailySteps = data.NumberOfSteps?.Int32Value ?? 0;
                    int previousDaily = Preferences.Get("DailySteps", 0);
                    int accumulated = Preferences.Get("AccumulatedSteps", 0);

                    int delta = dailySteps - previousDaily;

                    accumulated += delta;

                    Preferences.Set("AccumulatedSteps", accumulated);
                    Preferences.Set("DailySteps", dailySteps);
                    UpdateStepHistory(today, dailySteps);

                    StepsUpdated?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        public Task StopAsync()
        {
            _pedometer.StopPedometerUpdates();
            return Task.CompletedTask;
        }

        public void ResetAll()
        {
            Preferences.Set("AccumulatedSteps", 0);
            Preferences.Set("DailySteps", 0);
            Preferences.Set("StepHistory", "{}");
            Preferences.Set("StepCounterDate", DateTime.UtcNow.ToString("yyyy-MM-dd"));
        }

        private Dictionary<string, int> GetStepHistory()
        {
            var json = Preferences.Get("StepHistory", "{}");
            var history = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                          ?? new Dictionary<string, int>();
            return history;
        }

        private void UpdateStepHistory(string date, int stepsToday)
        {
            var json = Preferences.Get("StepHistory", "{}");
            var history = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                          ?? new Dictionary<string, int>();

            history[date] = stepsToday;

            var updatedJson = System.Text.Json.JsonSerializer.Serialize(history);
            Preferences.Set("StepHistory", updatedJson);
        }
    }
}