using Android.App;
using Android.Content;
using Android.OS;
using MAUI_Nonsense_App.Services;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using AApp = Android.App.Application;

namespace MAUI_Nonsense_App.Platforms.Android.Services.StepCounter
{
    public class AndroidStepCounterService : IStepCounterService
    {
        private readonly Context _context;
        public static AndroidStepCounterService? Instance { get; private set; }

        // -------- Public props --------
        public int TotalSteps => Preferences.Get("AccumulatedSteps", 0);
        public int Last24HoursSteps => Preferences.Get("DailySteps", 0);
        public long ActiveSecondsToday => Preferences.Get("ActiveSecondsToday", 0L);

        public Dictionary<string, int> StepHistory => GetStepHistoryDaily();
        public Dictionary<string, int> StepHistoryDaily => GetStepHistoryDaily();
        public Dictionary<string, int[]> StepHistoryHourly => GetStepHistoryHourly();

        public DateTime InstallDate
        {
            get
            {
                var v = Preferences.Get("InstallDate", "");
                if (DateTime.TryParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                    return dt;
                return DateTime.Now.Date;
            }
        }

        public event EventHandler? StepsUpdated;

        public AndroidStepCounterService()
        {
            _context = AApp.Context;
            Instance = this;
        }

        public async Task StartAsync()
        {
            // mark install date once
            if (!Preferences.ContainsKey("InstallDate"))
                Preferences.Set("InstallDate", DateTime.Now.Date.ToString("yyyy-MM-dd"));

            await Task.Delay(600); // small delay after boot/unlock
            StartForegroundService();
            InitializeMidnightBaselineIfMissing();
            EnsureTodayDailyExists();
            ScheduleMidnightReset();
            RaiseStepsUpdated();
        }

        public Task StopAsync()
        {
            StopForegroundService();
            return Task.CompletedTask;
        }

        public void ResetToday()
        {
            string today = DateTime.Now.Date.ToString("yyyy-MM-dd");
            int currentSensorValue = Preferences.Get("LastSensorReading", 0);

            // Re-baseline to current sensor reading so next tick starts at 0
            Preferences.Set("MidnightStepSensorValue", currentSensorValue);
            Preferences.Set("LastStepDate", today);

            // Clear today's counters
            Preferences.Set("DailySteps", 0);
            Preferences.Set("ActiveSecondsToday", 0L);
            Preferences.Set("LastStepUnixMs", 0L);
            Preferences.Set("RebootDailyOffset", 0);

            // Daily history -> 0
            var daily = GetStepHistoryDaily();
            daily[today] = 0;
            SaveStepHistoryDaily(daily);

            // Hourly history -> zero 24 slots
            var hourly = GetStepHistoryHourly();
            hourly[today] = new int[24];
            SaveStepHistoryHourly(hourly);

            RaiseStepsUpdated();
        }

        public void ResetAll()
        {
            Preferences.Set("AccumulatedSteps", 0);
            Preferences.Set("DailySteps", 0);
            Preferences.Set("ActiveSecondsToday", 0L);
            Preferences.Remove("FirstEverStepSensorValue");
            Preferences.Remove("MidnightStepSensorValue");
            Preferences.Set("StepHistoryDaily", "{}");
            Preferences.Set("StepHistoryHourly", "{}");
            Preferences.Set("LastStepDate", DateTime.Now.ToString("yyyy-MM-dd"));
            Preferences.Set("RebootDailyOffset", 0);
            Preferences.Set("RunningTotalSteps", 0);
            Preferences.Set("LastSensorReading", 0);
            Preferences.Set("LastStepUnixMs", 0L);

            // Recreate empty "today" entries so UI immediately sees zeros
            var todayKey = DateTime.Now.Date.ToString("yyyy-MM-dd");

            // daily
            var daily = GetStepHistoryDaily();
            daily[todayKey] = 0;
            SaveStepHistoryDaily(daily);

            // hourly
            var hourly = GetStepHistoryHourly();
            hourly[todayKey] = new int[24];
            SaveStepHistoryHourly(hourly);

            RaiseStepsUpdated();
        }

        public void RaiseStepsUpdated() => StepsUpdated?.Invoke(this, EventArgs.Empty);

        // ---------- Helpers exposed to UI ----------

        public int[] GetHourlySteps(DateTime localDate)
        {
            var key = localDate.ToString("yyyy-MM-dd");
            var hourly = GetStepHistoryHourly();
            if (!hourly.TryGetValue(key, out var arr) || arr == null || arr.Length != 24)
                return new int[24];
            return (int[])arr.Clone();
        }

        public IEnumerable<(DateTime WeekStart, int TotalSteps)> EnumerateWeeklyTotals(DayOfWeek weekStart)
        {
            var daily = GetStepHistoryDaily()
                .Select(kv => (Date: DateTime.ParseExact(kv.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture), Steps: kv.Value))
                .OrderBy(t => t.Date)
                .ToArray();

            if (daily.Length == 0) yield break;

            DateTime start = StartOfWeek(daily.First().Date, weekStart);
            DateTime end = StartOfWeek(DateTime.Now.Date, weekStart);

            for (var ws = start; ws <= end; ws = ws.AddDays(7))
            {
                var we = ws.AddDays(6);
                int sum = daily.Where(d => d.Date >= ws && d.Date <= we).Sum(d => d.Steps);
                yield return (ws, sum);
            }
        }

        // ---------- Private internals ----------

        private static DateTime StartOfWeek(DateTime dt, DayOfWeek start)
        {
            int diff = (7 + (dt.DayOfWeek - start)) % 7;
            return dt.AddDays(-diff).Date;
        }

        private void InitializeMidnightBaselineIfMissing()
        {
            string today = DateTime.Now.Date.ToString("yyyy-MM-dd");
            string lastDate = Preferences.Get("LastStepDate", "");

            if (lastDate != today)
            {
                Preferences.Set("LastStepDate", today);
            }

            if (!Preferences.ContainsKey("MidnightStepSensorValue"))
            {
                int currentValue = Preferences.Get("LastSensorReading", 0);
                Preferences.Set("MidnightStepSensorValue", currentValue);
            }

            // Ensure hourly array for today exists
            var hourly = GetStepHistoryHourly();
            if (!hourly.ContainsKey(today))
            {
                hourly[today] = new int[24];
                SaveStepHistoryHourly(hourly);
            }
        }

        private void EnsureTodayDailyExists()
        {
            string today = DateTime.Now.Date.ToString("yyyy-MM-dd");
            var daily = GetStepHistoryDaily();
            if (!daily.ContainsKey(today))
            {
                daily[today] = Preferences.Get("DailySteps", 0);
                SaveStepHistoryDaily(daily);
            }
        }

        private void ScheduleMidnightReset()
        {
            var intent = new Intent(_context, typeof(MidnightResetReceiver));
            var pi = PendingIntent.GetBroadcast(_context, 0, intent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

            var alarmManager = (AlarmManager)_context.GetSystemService(Context.AlarmService);
            var cal = Java.Util.Calendar.Instance;
            cal.TimeInMillis = Java.Lang.JavaSystem.CurrentTimeMillis();
            cal.Set(Java.Util.CalendarField.HourOfDay, 0);
            cal.Set(Java.Util.CalendarField.Minute, 0);
            cal.Set(Java.Util.CalendarField.Second, 0);
            cal.Add(Java.Util.CalendarField.DayOfYear, 1);

            alarmManager.SetInexactRepeating(
                AlarmType.RtcWakeup,
                cal.TimeInMillis,
                AlarmManager.IntervalDay,
                pi);
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

        // ---- Storage (daily & hourly) ----

        private Dictionary<string, int> GetStepHistoryDaily()
        {
            var json = Preferences.Get("StepHistoryDaily",
                        Preferences.Get("StepHistory", "{}")); // migrate older key
            var history = JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                          ?? new Dictionary<string, int>();
            // migrate if old key used
            if (!Preferences.ContainsKey("StepHistoryDaily"))
                Preferences.Set("StepHistoryDaily", JsonSerializer.Serialize(history));
            return history;
        }

        private void SaveStepHistoryDaily(Dictionary<string, int> daily)
            => Preferences.Set("StepHistoryDaily", JsonSerializer.Serialize(daily));

        private Dictionary<string, int[]> GetStepHistoryHourly()
        {
            var json = Preferences.Get("StepHistoryHourly", "{}");
            var history = JsonSerializer.Deserialize<Dictionary<string, int[]>>(json)
                          ?? new Dictionary<string, int[]>();
            return history;
        }

        private void SaveStepHistoryHourly(Dictionary<string, int[]> hourly)
            => Preferences.Set("StepHistoryHourly", JsonSerializer.Serialize(hourly));
    }
}
