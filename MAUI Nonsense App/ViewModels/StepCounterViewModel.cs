using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using MAUI_Nonsense_App.Services;
using Microsoft.Maui.Storage;

namespace MAUI_Nonsense_App.ViewModels
{
    public class StepDay
    {
        public string Date { get; set; } = "";
        public int Steps { get; set; }
    }

    public class WeekDayItem : INotifyPropertyChanged
    {
        public string Abbrev { get; set; } = "";
        public int Steps { get; set; }
        public double Progress { get; set; }   // 0..1 of goal
        public bool Achieved { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class StepCounterViewModel : INotifyPropertyChanged
    {
        private readonly IStepCounterService _service;

        // backing
        private int _todaySteps;
        private int _dailyGoal;
        private long _activeSeconds; // kept for future use
        private double _distanceKm;
        private double _calories;
        private double _goalProgress;
        private string _activeTimeText = "0h 0m";

        // today bindings
        public int TodaySteps { get => _todaySteps; set { if (_todaySteps != value) { _todaySteps = value; OnChanged(nameof(TodaySteps)); RecalcDerived(); } } }
        public int DailyGoal { get => _dailyGoal; set { if (_dailyGoal != value) { _dailyGoal = value; OnChanged(nameof(DailyGoal)); RecalcDerived(); BuildWeekStrip(); } } }
        public long ActiveSeconds { get => _activeSeconds; set { if (_activeSeconds != value) { _activeSeconds = value; OnChanged(nameof(ActiveSeconds)); } } }

        public double DistanceKm { get => _distanceKm; private set { if (Math.Abs(_distanceKm - value) > 1e-6) { _distanceKm = value; OnChanged(nameof(DistanceKm)); } } }
        public double Calories { get => _calories; private set { if (Math.Abs(_calories - value) > 1e-6) { _calories = value; OnChanged(nameof(Calories)); } } }
        public double GoalProgress { get => _goalProgress; private set { if (Math.Abs(_goalProgress - value) > 1e-6) { _goalProgress = value; OnChanged(nameof(GoalProgress)); OnChanged(nameof(GoalProgressPctWidth)); } } }
        public string ActiveTimeText { get => _activeTimeText; private set { if (_activeTimeText != value) { _activeTimeText = value; OnChanged(nameof(ActiveTimeText)); } } }

        // progress bar helper
        public double GoalProgressPctWidth => Math.Clamp(GoalProgress, 0, 1) * 300;

        // last 7 days + weekly strip
        public ObservableCollection<StepDay> Last7Days { get; } = new();
        public ObservableCollection<WeekDayItem> WeekDays { get; } = new();

        // DAILY AVERAGE (all-time daily average)
        public int SevenDayAverage { get; private set; }
        public double SevenDayAvgDistanceKm { get; private set; }
        public double SevenDayAvgCalories { get; private set; }
        public string SevenDayAvgTimeText { get; private set; } = "0h 0m";

        // ALL-TIME TOTALS (for the new card)
        public int AllTimeSteps { get; private set; }
        public double AllTimeDistanceKm { get; private set; }
        public double AllTimeCalories { get; private set; }

        // helpers
        public DateTime InstallDate => _service.InstallDate;
        public int[] GetHourlySteps(DateTime localDate) => _service.GetHourlySteps(localDate);
        public IEnumerable<(DateTime WeekStart, int TotalSteps)> AllWeeks(DayOfWeek weekStart)
            => _service.EnumerateWeeklyTotals(weekStart);

        // settings keys
        private const string KeyStrideLengthCm = "Settings.StrideLengthCm";
        private const string KeyDailyGoal = "Settings.DailyGoal";
        private const string KeyWeightKg = "Settings.WeightKg";

        public event PropertyChangedEventHandler? PropertyChanged;

        public StepCounterViewModel(IStepCounterService service)
        {
            _service = service;

            ReloadSettings();

            // initial load
            TodaySteps = _service.Last24HoursSteps;
            ActiveSeconds = _service.ActiveSecondsToday;
            LoadLast7Days();
            BuildWeekStrip();
            RecalcAllTimeTotals();

            _service.StepsUpdated += (s, e) =>
            {
                TodaySteps = _service.Last24HoursSteps;
                ActiveSeconds = _service.ActiveSecondsToday;
                LoadLast7Days();
                BuildWeekStrip();
                RecalcAllTimeTotals();
            };
        }

        public void ReloadSettings()
        {
            DailyGoal = Preferences.Get(KeyDailyGoal, 10000);
            RecalcDerived();
        }

        public void ReloadLast7Days() { LoadLast7Days(); BuildWeekStrip(); RecalcAllTimeTotals(); }

        private void LoadLast7Days()
        {
            Last7Days.Clear();
            var history = _service.StepHistoryDaily;

            // Fill the "last 7 days" list (for recap)
            for (int i = 0; i < 7; i++)
            {
                var key = DateTime.Now.Date.AddDays(-i).ToString("yyyy-MM-dd");
                history.TryGetValue(key, out int steps);
                Last7Days.Add(new StepDay { Date = key, Steps = steps });
            }

            // ---------- ALL-TIME DAILY AVERAGE (same logic as report page) ----------
            DateTime today = DateTime.Now.Date;
            DateTime first;
            if (history.Count > 0)
            {
                first = history.Keys
                               .Select(k => DateTime.ParseExact(k, "yyyy-MM-dd", CultureInfo.InvariantCulture))
                               .Min()
                               .Date;
            }
            else
            {
                first = InstallDate.Date;
            }

            int daySpan = Math.Max(1, (today - first).Days + 1);

            long totalSteps = history.Values.Sum(v => (long)v);
            if (history.Count == 0)
                totalSteps = _service.Last24HoursSteps;

            SevenDayAverage = (int)Math.Round(totalSteps / (double)daySpan, MidpointRounding.AwayFromZero);

            int strideCm = Preferences.Get(KeyStrideLengthCm, 75);
            var avgMinutes = SevenDayAverage / 100.0;
            SevenDayAvgDistanceKm = SevenDayAverage * (strideCm / 100000.0);
            SevenDayAvgCalories = MinutesToCalories(avgMinutes);

            var h = (int)(avgMinutes / 60.0);
            var m = (int)Math.Round(avgMinutes - h * 60);
            SevenDayAvgTimeText = $"{h}h {m}m";

            OnChanged(nameof(SevenDayAverage));
            OnChanged(nameof(SevenDayAvgDistanceKm));
            OnChanged(nameof(SevenDayAvgCalories));
            OnChanged(nameof(SevenDayAvgTimeText));
        }

        private void BuildWeekStrip()
        {
            WeekDays.Clear();

            var start = StartOfWeek(DateTime.Now.Date, CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
            var abbrevCulture = new CultureInfo("en-US");

            for (int i = 0; i < 7; i++)
            {
                var day = start.AddDays(i);
                var key = day.ToString("yyyy-MM-dd");
                _service.StepHistoryDaily.TryGetValue(key, out int steps);

                var prog = DailyGoal > 0 ? Math.Clamp(steps / (double)DailyGoal, 0, 1) : 0;

                WeekDays.Add(new WeekDayItem
                {
                    Abbrev = abbrevCulture.DateTimeFormat.GetAbbreviatedDayName(day.DayOfWeek),
                    Steps = steps,
                    Progress = prog,
                    Achieved = steps >= DailyGoal
                });
            }
            OnChanged(nameof(WeekDays));
        }

        private static DateTime StartOfWeek(DateTime dt, DayOfWeek start)
        {
            int diff = (7 + (dt.DayOfWeek - start)) % 7;
            return dt.AddDays(-diff).Date;
        }

        private void RecalcDerived()
        {
            // conversions from steps:
            int strideCm = Preferences.Get(KeyStrideLengthCm, 75);

            DistanceKm = TodaySteps * (strideCm / 100000.0);

            var minutes = TodaySteps / 100.0;
            var h = (int)(minutes / 60.0);
            var m = (int)Math.Round(minutes - h * 60);
            ActiveTimeText = $"{h}h {m}m";

            Calories = MinutesToCalories(minutes);

            GoalProgress = DailyGoal > 0 ? Math.Clamp(TodaySteps / (double)DailyGoal, 0, 1) : 0;
        }

        private void RecalcAllTimeTotals()
        {
            var history = _service.StepHistoryDaily;
            var todayKey = DateTime.Now.Date.ToString("yyyy-MM-dd");

            long total = history.Values.Sum(v => (long)v);
            if (!history.ContainsKey(todayKey))
                total += _service.Last24HoursSteps;

            AllTimeSteps = (int)Math.Min(total, int.MaxValue);

            int strideCm = Preferences.Get(KeyStrideLengthCm, 75);
            AllTimeDistanceKm = AllTimeSteps * (strideCm / 100000.0);

            var minutes = AllTimeSteps / 100.0;
            AllTimeCalories = MinutesToCalories(minutes);

            OnChanged(nameof(AllTimeSteps));
            OnChanged(nameof(AllTimeDistanceKm));
            OnChanged(nameof(AllTimeCalories));
        }

        private double MinutesToCalories(double minutes)
        {
            // kcal/min = MET(=3.5) * 3.5 * kg / 200
            var kg = Preferences.Get(KeyWeightKg, 70.0);
            var kcalPerMin = 3.5 * 3.5 * kg / 200.0;
            return minutes * kcalPerMin;
        }

        protected void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
