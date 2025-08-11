using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreMotion;
using Foundation;
using Microsoft.Maui.Storage;
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

            var calendar = NSCalendar.CurrentCalendar;
            var now = NSDate.Now;
            var todayMidnight = calendar.StartOfDayForDate(now);
            var todayKey = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

            // Handle day rollover while the app wasn't running:
            string storedDate = Preferences.Get("StepCounterDate", todayKey);
            if (storedDate != todayKey)
            {
                // Write accurate yesterday steps using a pedometer query
                var yesterdayKey = storedDate;
                var yesterdayMidnight = calendar.StartOfDayForDate(now.AddSeconds(-24 * 60 * 60));
                var todayAtMidnightLocal = todayMidnight;

                try
                {
                    var yesterdayData = await _pedometer.QueryPedometerDataAsync(yesterdayMidnight, todayAtMidnightLocal);
                    int yesterdaySteps = (int)(yesterdayData?.NumberOfSteps?.Int32Value ?? 0);
                    UpdateStepHistory(yesterdayKey, yesterdaySteps);
                }
                catch
                {
                    // If the query fails, don't overwrite—better to keep existing value than set 0.
                }

                Preferences.Set("StepCounterDate", todayKey);
                Preferences.Set("DailySteps", 0); // will be immediately replaced by the initial query below
            }

            // Initial seed: query today's steps up to now so UI is correct immediately
            try
            {
                var seedData = await _pedometer.QueryPedometerDataAsync(todayMidnight, now);
                int seedDaily = Math.Max(0, seedData?.NumberOfSteps?.Int32Value ?? 0);

                int prevDaily = Preferences.Get("DailySteps", 0);
                int accumulated = Preferences.Get("AccumulatedSteps", 0);

                int delta = seedDaily - prevDaily;
                if (delta > 0)
                    accumulated += delta;

                Preferences.Set("AccumulatedSteps", accumulated);
                Preferences.Set("DailySteps", seedDaily);
                Preferences.Set("StepCounterDate", todayKey);
                UpdateStepHistory(todayKey, seedDaily);

                RaiseStepsUpdated();
            }
            catch
            {
                // If seed query fails, we still start live updates below
            }

            // Live updates from midnight → now
            _pedometer.StartPedometerUpdates(todayMidnight, (data, error) =>
            {
                if (data == null || error != null) return;

                int liveDaily = Math.Max(0, data.NumberOfSteps?.Int32Value ?? 0);

                int previousDaily = Preferences.Get("DailySteps", 0);
                int accumulated = Preferences.Get("AccumulatedSteps", 0);

                int delta = liveDaily - previousDaily;
                if (delta > 0)
                    accumulated += delta;

                Preferences.Set("AccumulatedSteps", accumulated);
                Preferences.Set("DailySteps", liveDaily);
                Preferences.Set("StepCounterDate", todayKey);
                UpdateStepHistory(todayKey, liveDaily);

                RaiseStepsUpdated();
            });

            // (Optional) Backfill last 7 days history in the background (fire-and-forget)
            _ = BackfillLast7DaysAsync();
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

        public void RaiseStepsUpdated() => StepsUpdated?.Invoke(this, EventArgs.Empty);

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

            // (Optional) keep only 7 most-recent days
            TrimHistoryToLastDays(history, 7);

            Preferences.Set("StepHistory", System.Text.Json.JsonSerializer.Serialize(history));
        }

        private static void TrimHistoryToLastDays(Dictionary<string, int> history, int days)
        {
            try
            {
                var keep = new HashSet<string>();
                for (int i = 0; i < days; i++)
                    keep.Add(DateTime.UtcNow.Date.AddDays(-i).ToString("yyyy-MM-dd"));

                var keys = new List<string>(history.Keys);
                foreach (var k in keys)
                    if (!keep.Contains(k))
                        history.Remove(k);
            }
            catch { /* best effort */ }
        }

        private async Task BackfillLast7DaysAsync()
        {
            try
            {
                if (!CMPedometer.IsStepCountingAvailable) return;

                var calendar = NSCalendar.CurrentCalendar;
                var now = NSDate.Now;
                var todayMidnight = calendar.StartOfDayForDate(now);

                for (int i = 1; i <= 6; i++)
                {
                    var dayEnd = todayMidnight.AddSeconds(-24 * 60 * 60 * (i - 1));
                    var dayStart = todayMidnight.AddSeconds(-24 * 60 * 60 * i);

                    var data = await _pedometer.QueryPedometerDataAsync(dayStart, dayEnd);
                    int steps = Math.Max(0, data?.NumberOfSteps?.Int32Value ?? 0);

                    var key = DateTime.UtcNow.Date.AddDays(-i).ToString("yyyy-MM-dd");
                    UpdateStepHistory(key, steps);
                }
            }
            catch
            {
                // If any query fails, ignore; history will still be partially correct.
            }
        }
    }
}
