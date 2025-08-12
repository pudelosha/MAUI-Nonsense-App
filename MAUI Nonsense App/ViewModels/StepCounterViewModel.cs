using System.Collections.ObjectModel;
using System.ComponentModel;
using MAUI_Nonsense_App.Services;
using Microsoft.Maui.Storage;
using System.Globalization;

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

        // ---- backing
        private int _todaySteps;
        private int _dailyGoal;
        private long _activeSeconds;
        private double _distanceKm;
        private double _calories;
        private double _goalProgress;
        private string _activeTimeText = "0h 0m";

        // ---- binding
        public int TodaySteps { get => _todaySteps; set { if (_todaySteps != value) { _todaySteps = value; OnChanged(nameof(TodaySteps)); RecalcDerived(); } } }
        public int DailyGoal { get => _dailyGoal; set { if (_dailyGoal != value) { _dailyGoal = value; OnChanged(nameof(DailyGoal)); RecalcDerived(); } } }
        public long ActiveSeconds { get => _activeSeconds; set { if (_activeSeconds != value) { _activeSeconds = value; OnChanged(nameof(ActiveSeconds)); RecalcDerived(); } } }

        public double DistanceKm { get => _distanceKm; private set { if (Math.Abs(_distanceKm - value) > 1e-6) { _distanceKm = value; OnChanged(nameof(DistanceKm)); } } }
        public double Calories { get => _calories; private set { if (Math.Abs(_calories - value) > 1e-6) { _calories = value; OnChanged(nameof(Calories)); } } }
        public double GoalProgress { get => _goalProgress; private set { if (Math.Abs(_goalProgress - value) > 1e-6) { _goalProgress = value; OnChanged(nameof(GoalProgress)); OnChanged(nameof(GoalProgressPctWidth)); } } }
        public string ActiveTimeText { get => _activeTimeText; private set { if (_activeTimeText != value) { _activeTimeText = value; OnChanged(nameof(ActiveTimeText)); } } }

        // width helper for top progress bar (updated in code-behind according to actual width)
        // We'll compute it as percentage of an assumed 100 pixels; the Grid stretches it.
        public double GoalProgressPctWidth => Math.Clamp(GoalProgress, 0, 1) * 300; // 300px "virtual" width

        public ObservableCollection<StepDay> Last7Days { get; } = new();
        public ObservableCollection<WeekDayItem> WeekDays { get; } = new();

        public int SevenDayAverage { get; private set; }

        // ---- constants & settings keys
        private const string KeyStrideLengthCm = "Settings.StrideLengthCm";
        private const string KeyManualStrideOverride = "Settings.ManualStrideOverride";
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

            _service.StepsUpdated += (s, e) =>
            {
                TodaySteps = _service.Last24HoursSteps;
                ActiveSeconds = _service.ActiveSecondsToday;
                LoadLast7Days();
                BuildWeekStrip();
            };
        }

        public void ReloadSettings()
        {
            DailyGoal = Preferences.Get(KeyDailyGoal, 5000);
            RecalcDerived();
        }

        public void ReloadLast7Days() { LoadLast7Days(); BuildWeekStrip(); }

        private void LoadLast7Days()
        {
            Last7Days.Clear();
            var history = _service.StepHistory;

            int sum = 0;
            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i).ToString("yyyy-MM-dd");
                history.TryGetValue(date, out int steps);

                Last7Days.Add(new StepDay { Date = date, Steps = steps });
                sum += steps;
            }
            SevenDayAverage = (int)Math.Round(sum / 7.0, MidpointRounding.AwayFromZero);
            OnChanged(nameof(SevenDayAverage));
        }

        private void BuildWeekStrip()
        {
            WeekDays.Clear();

            var cultureForStart = CultureInfo.CurrentCulture;
            var abbrevCulture = new CultureInfo("en-US");   // force English labels

            var firstDay = cultureForStart.DateTimeFormat.FirstDayOfWeek;
            var dt = StartOfWeek(DateTime.UtcNow.Date, firstDay);

            for (int i = 0; i < 7; i++)
            {
                var day = dt.AddDays(i);
                var key = day.ToString("yyyy-MM-dd");
                _service.StepHistory.TryGetValue(key, out int steps);

                var prog = DailyGoal > 0 ? Math.Clamp(steps / (double)DailyGoal, 0, 1) : 0;

                WeekDays.Add(new WeekDayItem
                {
                    Abbrev = abbrevCulture.DateTimeFormat.GetAbbreviatedDayName(day.DayOfWeek), // Mon, Tue...
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
            return dt.AddDays(-1 * diff).Date;
        }

        private void RecalcDerived()
        {
            // stride length (cm) -> distance
            int strideCm;
            if (Preferences.ContainsKey(KeyStrideLengthCm))
                strideCm = Preferences.Get(KeyStrideLengthCm, 75);
            else
                strideCm = 75; // default

            var meters = TodaySteps * (strideCm / 100.0);
            DistanceKm = meters / 1000.0;

            // time
            var secs = Math.Max(0, ActiveSeconds);
            var h = secs / 3600;
            var m = (secs % 3600) / 60;
            ActiveTimeText = $"{h}h {m}m";

            // calories: MET * kg * hours ; MET estimated from speed = distance / time
            var kg = Preferences.Get(KeyWeightKg, 70.0);
            var hours = secs / 3600.0;
            double met = 3.5; // default moderate walk
            if (hours > 0.0)
            {
                var kmh = DistanceKm / hours;
                met = kmh switch
                {
                    < 3.2 => 2.5,
                    < 4.0 => 2.8,
                    < 4.8 => 3.3,
                    < 5.6 => 4.0,
                    < 6.4 => 4.8,
                    < 7.2 => 6.3,
                    _ => 7.0
                };
            }
            Calories = kg * met * hours;

            GoalProgress = DailyGoal > 0 ? Math.Clamp(TodaySteps / (double)DailyGoal, 0, 1) : 0;
        }

        protected void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
