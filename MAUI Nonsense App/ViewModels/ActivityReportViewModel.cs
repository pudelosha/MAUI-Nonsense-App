using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Pages.Activity
{
    public enum MetricMode { Steps, Calories, Time, Distance }

    public class DayStat
    {
        public DateTime Date { get; set; }
        public string Abbrev => Date.ToString("ddd", CultureInfo.InvariantCulture);
        public int Steps { get; set; }
        public double DistanceKm { get; set; }
        public double Calories { get; set; }
        public double Minutes { get; set; }
        public bool IsToday { get; set; }
        public bool IsFuture { get; set; }
        public bool HasData { get; set; }     // <-- NEW: day exists in history (or is today)
    }

    public class ActivityReportViewModel : INotifyPropertyChanged
    {
        private readonly IStepCounterService _service;

        private const string KeyWeekStart = "Settings.WeekStart";
        private const string KeyDailyGoal = "Settings.DailyGoal";
        private const string KeyStrideLengthCm = "Settings.StrideLengthCm";
        private const string KeyWeightKg = "Settings.WeightKg";

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? RedrawRequested;

        public ObservableCollection<DayStat> Days { get; } = new();

        public DateTime WeekStart { get; private set; }
        public DateTime WeekEnd => WeekStart.AddDays(6);

        // ---- Top card (uses selected metric)
        public string TopPrimaryValueText
        {
            get
            {
                var steps = _service.Last24HoursSteps;
                return SelectedMode switch
                {
                    MetricMode.Steps => steps.ToString("N0"),
                    MetricMode.Distance => (steps * _strideCm / 100_000.0).ToString("F2"),
                    MetricMode.Time => (steps / 100.0).ToString("N0"),
                    _ => MinutesToCalories(steps / 100.0).ToString("F0")
                };
            }
        }

        // Average for the current week **only over days that have data**
        public string TopAverageText
        {
            get
            {
                var today = DateTime.UtcNow.Date;
                var end = WeekEnd <= today ? WeekEnd : (today < WeekStart ? WeekStart.AddDays(-1) : today);

                int daysCount = 0;
                double sum = 0;

                foreach (var d in Days)
                {
                    if (d.Date >= WeekStart && d.Date <= end && d.HasData)
                    {
                        daysCount++;
                        sum += ValueOf(d);
                    }
                }

                if (daysCount <= 0) return "0";

                var avg = sum / daysCount;
                return SelectedMode switch
                {
                    MetricMode.Steps => avg.ToString("N0"),
                    MetricMode.Distance => avg.ToString("F2"),
                    MetricMode.Time => avg.ToString("N0"),
                    _ => avg.ToString("F0")
                };
            }
        }

        public string WeekRangeText => $"{WeekStart:dd.MM} - {WeekEnd:dd.MM}";

        // navigation guard (no future)
        public bool CanGoForward
        {
            get
            {
                var currentStart = GetWeekStartOf(DateTime.UtcNow.Date);
                return WeekStart < currentStart;
            }
        }

        // ---- Selected metric for chart + top card
        private MetricMode _selectedMode = MetricMode.Steps;
        public MetricMode SelectedMode
        {
            get => _selectedMode;
            set
            {
                if (_selectedMode == value) return;
                _selectedMode = value;
                OnPropertyChanged(nameof(SelectedMode));
                OnPropertyChanged(nameof(TopPrimaryValueText));
                OnPropertyChanged(nameof(TopAverageText));
                RedrawRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        public IRelayCommand SetModeCommand { get; }

        // ---- Totals (optional)
        public int WeeklySumSteps => Days.Sum(d => d.Steps);
        public double WeeklySumDistanceKm => Days.Sum(d => d.DistanceKm);
        public double WeeklySumMinutes => Days.Sum(d => d.Minutes);
        public double WeeklySumCalories => Days.Sum(d => d.Calories);

        // ---- Goals / conversions
        private double _strideCm;
        private double _weightKg;

        public int DailyGoalSteps { get; private set; }
        public double GoalDistanceKm => DailyGoalSteps * _strideCm / 100_000.0;
        public double GoalMinutes => DailyGoalSteps / 100.0; // 100 steps/min
        public double GoalCalories => MinutesToCalories(GoalMinutes);

        private readonly bool _isMondayStart;

        public ActivityReportViewModel(IStepCounterService service)
        {
            _service = service;

            _isMondayStart = Preferences.Get(KeyWeekStart, "Monday")
                                        .Equals("Monday", StringComparison.OrdinalIgnoreCase);

            WeekStart = GetWeekStartOf(DateTime.UtcNow.Date);

            DailyGoalSteps = Preferences.Get(KeyDailyGoal, 10000);
            _strideCm = Preferences.ContainsKey(KeyStrideLengthCm) ? Preferences.Get(KeyStrideLengthCm, 75) : 75;
            _weightKg = Preferences.ContainsKey(KeyWeightKg) ? Preferences.Get(KeyWeightKg, 70.0) : 70.0;

            SetModeCommand = new RelayCommand<string>(s =>
            {
                if (Enum.TryParse<MetricMode>(s, true, out var m))
                    SelectedMode = m;
            });

            _service.StepsUpdated += (_, __) =>
            {
                if (DateTime.UtcNow.Date >= WeekStart && DateTime.UtcNow.Date <= WeekEnd)
                    LoadWeek();
                OnPropertyChanged(nameof(TopPrimaryValueText));
                OnPropertyChanged(nameof(TopAverageText));
            };

            LoadWeek();
        }

        private DateTime GetWeekStartOf(DateTime date)
            => _isMondayStart
               ? date.AddDays(-(int)((((int)date.DayOfWeek + 6) % 7))) // Monday=0
               : date.AddDays(-(int)date.DayOfWeek);                   // Sunday=0

        public void TryShiftWeek(int deltaWeeks)
        {
            if (deltaWeeks > 0 && !CanGoForward) return; // block future
            WeekStart = WeekStart.AddDays(7 * deltaWeeks);
            LoadWeek();
        }

        private void LoadWeek()
        {
            Days.Clear();
            var history = _service.StepHistory;
            var today = DateTime.UtcNow.Date;

            for (int i = 0; i < 7; i++)
            {
                var day = WeekStart.AddDays(i);
                var key = day.ToString("yyyy-MM-dd");

                bool has = history.ContainsKey(key) || day == today; // <-- NEW: only count if present (or today)
                history.TryGetValue(key, out var steps);

                var distKm = steps * _strideCm / 100000.0;
                var minutes = steps / 100.0;
                var kcal = MinutesToCalories(minutes);

                Days.Add(new DayStat
                {
                    Date = day,
                    Steps = steps,
                    DistanceKm = distKm,
                    Minutes = minutes,
                    Calories = kcal,
                    IsToday = day == today,
                    IsFuture = day > today,
                    HasData = has
                });
            }

            OnPropertyChanged(nameof(WeekRangeText));
            OnPropertyChanged(nameof(TopAverageText));
            OnPropertyChanged(nameof(CanGoForward));
            RedrawRequested?.Invoke(this, EventArgs.Empty);
        }

        private double ValueOf(DayStat d) => SelectedMode switch
        {
            MetricMode.Steps => d.Steps,
            MetricMode.Distance => d.DistanceKm,
            MetricMode.Time => d.Minutes,
            _ => d.Calories
        };

        private double MinutesToCalories(double minutes)
        {
            // kcal/min = MET * 3.5 * kg / 200 ; walking easy MET ~ 3.5
            var kcalPerMin = 3.5 * 3.5 * _weightKg / 200.0;
            return minutes * kcalPerMin;
        }

        protected void OnPropertyChanged(string n)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
