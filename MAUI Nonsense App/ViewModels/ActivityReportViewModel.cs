using System;
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
    public enum ReportRange { Day, Week, Month }

    public class DayStat
    {
        public DateTime Date { get; set; }
        public int Steps { get; set; }
        public double DistanceKm { get; set; }
        public double Calories { get; set; }
        public double Minutes { get; set; }
        public bool HasData { get; set; }
        public bool IsToday { get; set; }
        public bool IsFuture { get; set; }

        public string Abbrev => Date.ToString("ddd", CultureInfo.InvariantCulture);
    }

    public class ActivityReportViewModel : INotifyPropertyChanged
    {
        private readonly IStepCounterService _svc;

        private const string KeyWeekStart = "Settings.WeekStart";
        private const string KeyDailyGoal = "Settings.DailyGoal";
        private const string KeyStrideLengthCm = "Settings.StrideLengthCm";
        private const string KeyWeightKg = "Settings.WeightKg";

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? RedrawRequested;

        public ObservableCollection<DayStat> Days { get; } = new();        // Week view
        public ObservableCollection<DayStat> MonthDays { get; } = new();   // Month view
        public int[] HourlyStepsForDay { get; private set; } = new int[24]; // Day view

        private ReportRange _selectedRange = ReportRange.Week;
        public ReportRange SelectedRange
        {
            get => _selectedRange;
            set
            {
                if (_selectedRange == value) return;
                _selectedRange = value;
                OnPropertyChanged(nameof(SelectedRange));
                LoadRange();
                OnPropertyChanged(nameof(LeftLabel));
                OnPropertyChanged(nameof(RightLabel));
                OnPropertyChanged(nameof(LeftValueText));
                OnPropertyChanged(nameof(RightValueText));
            }
        }

        public DateTime AnchorDate { get; private set; }
        public DateTime WeekStart => GetWeekStartOf(AnchorDate);
        public DateTime WeekEnd => WeekStart.AddDays(6);
        public DateTime MonthStart => new DateTime(AnchorDate.Year, AnchorDate.Month, 1);
        public DateTime MonthEnd => MonthStart.AddMonths(1).AddDays(-1);

        public string LeftLabel => SelectedRange switch
        {
            ReportRange.Day => "Today's total",
            ReportRange.Week => "Weekly total",
            ReportRange.Month => "Monthly total",
            _ => ""
        };

        public string RightLabel => SelectedRange switch
        {
            ReportRange.Day => "All-time daily avg",
            ReportRange.Week => "Weekly average",
            ReportRange.Month => "Monthly average",
            _ => ""
        };

        public string LeftValueText => TopPrimaryValueText;
        public string RightValueText => TopAverageText;

        public string TopPrimaryValueText
        {
            get
            {
                double totalSteps = SelectedRange switch
                {
                    ReportRange.Day => GetDayTotalSteps(),
                    ReportRange.Week => Days.Sum(d => d.Steps),
                    ReportRange.Month => MonthDays.Sum(d => d.Steps),
                    _ => 0
                };

                return SelectedMode switch
                {
                    MetricMode.Steps => totalSteps.ToString("N0"),
                    MetricMode.Distance => (totalSteps * _strideCm / 100_000.0).ToString("F2"),
                    MetricMode.Time => (totalSteps / 100.0).ToString("N0"),
                    _ => MinutesToCalories(totalSteps / 100.0).ToString("F0")
                };
            }
        }

        public string TopAverageText
        {
            get
            {
                if (SelectedRange == ReportRange.Day)
                {
                    var avgSteps = GetAllTimeDailyAverageSteps();
                    return SelectedMode switch
                    {
                        MetricMode.Steps => avgSteps.ToString("N0"),
                        MetricMode.Distance => (avgSteps * _strideCm / 100_000.0).ToString("F2"),
                        MetricMode.Time => (avgSteps / 100.0).ToString("N0"),
                        _ => MinutesToCalories(avgSteps / 100.0).ToString("F0")
                    };
                }

                var set = SelectedRange == ReportRange.Week ? Days : MonthDays;
                var withData = set.Where(d => d.HasData).ToList();
                if (withData.Count == 0) return "0";

                double avgStepsRange = withData.Average(d => d.Steps);
                return SelectedMode switch
                {
                    MetricMode.Steps => avgStepsRange.ToString("N0"),
                    MetricMode.Distance => (avgStepsRange * _strideCm / 100_000.0).ToString("F2"),
                    MetricMode.Time => (avgStepsRange / 100.0).ToString("N0"),
                    _ => MinutesToCalories(avgStepsRange / 100.0).ToString("F0")
                };
            }
        }

        public string RangeText => SelectedRange switch
        {
            ReportRange.Day => $"{AnchorDate:ddd, dd MMM}",
            ReportRange.Week => $"{WeekStart:dd.MM} - {WeekEnd:dd.MM}",
            ReportRange.Month => $"{AnchorDate:MMMM yyyy}",
            _ => ""
        };

        public bool CanGoForward
        {
            get
            {
                var today = DateTime.UtcNow.Date;
                return SelectedRange switch
                {
                    ReportRange.Day => AnchorDate < today,
                    ReportRange.Week => WeekStart < GetWeekStartOf(today),
                    ReportRange.Month => MonthStart < new DateTime(today.Year, today.Month, 1),
                    _ => false
                };
            }
        }

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
                OnPropertyChanged(nameof(LeftValueText));
                OnPropertyChanged(nameof(RightValueText));
                RedrawRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        public IRelayCommand SetModeCommand { get; }
        public IRelayCommand SetRangeCommand { get; }

        private double _strideCm;
        private double _weightKg;

        public int DailyGoalSteps { get; private set; }
        public double GoalDistanceKm => DailyGoalSteps * _strideCm / 100_000.0;
        public double GoalMinutes => DailyGoalSteps / 100.0;
        public double GoalCalories => MinutesToCalories(GoalMinutes);

        private readonly bool _isMondayStart;

        public ActivityReportViewModel(IStepCounterService service)
        {
            _svc = service;

            _isMondayStart = Preferences.Get(KeyWeekStart, "Monday")
                                        .Equals("Monday", StringComparison.OrdinalIgnoreCase);

            AnchorDate = DateTime.UtcNow.Date;

            DailyGoalSteps = Preferences.Get(KeyDailyGoal, 10000);
            _strideCm = Preferences.ContainsKey(KeyStrideLengthCm) ? Preferences.Get(KeyStrideLengthCm, 75) : 75;
            _weightKg = Preferences.ContainsKey(KeyWeightKg) ? Preferences.Get(KeyWeightKg, 70.0) : 70.0;

            SetModeCommand = new RelayCommand<string>(s =>
            {
                if (Enum.TryParse<MetricMode>(s, true, out var m))
                    SelectedMode = m;
            });

            SetRangeCommand = new RelayCommand<string>(s =>
            {
                if (Enum.TryParse<ReportRange>(s, true, out var r))
                    SelectedRange = r;
            });

            _svc.StepsUpdated += (_, __) =>
            {
                LoadRange();
                OnPropertyChanged(nameof(TopPrimaryValueText));
                OnPropertyChanged(nameof(TopAverageText));
                OnPropertyChanged(nameof(LeftValueText));
                OnPropertyChanged(nameof(RightValueText));
            };

            LoadRange();
        }

        public void TryShiftRange(int delta)
        {
            switch (SelectedRange)
            {
                case ReportRange.Day: AnchorDate = AnchorDate.AddDays(delta); break;
                case ReportRange.Week: AnchorDate = AnchorDate.AddDays(7 * delta); break;
                case ReportRange.Month: AnchorDate = AnchorDate.AddMonths(delta); break;
            }
            LoadRange();
        }

        private void LoadRange()
        {
            switch (SelectedRange)
            {
                case ReportRange.Day: LoadDay(); break;
                case ReportRange.Month: LoadMonth(); break;
                default: LoadWeek(); break;
            }

            OnPropertyChanged(nameof(RangeText));
            OnPropertyChanged(nameof(CanGoForward));
            OnPropertyChanged(nameof(TopPrimaryValueText));
            OnPropertyChanged(nameof(TopAverageText));
            OnPropertyChanged(nameof(LeftLabel));
            OnPropertyChanged(nameof(RightLabel));
            OnPropertyChanged(nameof(LeftValueText));
            OnPropertyChanged(nameof(RightValueText));

            RedrawRequested?.Invoke(this, EventArgs.Empty);
        }

        private void LoadDay()
        {
            HourlyStepsForDay = _svc.GetHourlySteps(AnchorDate) ?? new int[24];

            OnPropertyChanged(nameof(TopPrimaryValueText));
            OnPropertyChanged(nameof(TopAverageText));
            OnPropertyChanged(nameof(LeftValueText));
            OnPropertyChanged(nameof(RightValueText));
        }

        private void LoadWeek()
        {
            Days.Clear();
            var history = _svc.StepHistory;
            var today = DateTime.UtcNow.Date;

            for (int i = 0; i < 7; i++)
            {
                var day = WeekStart.AddDays(i);
                var key = day.ToString("yyyy-MM-dd");
                bool has = history.ContainsKey(key) || day == today;
                history.TryGetValue(key, out var steps);

                Days.Add(new DayStat
                {
                    Date = day,
                    Steps = steps,
                    DistanceKm = steps * _strideCm / 100000.0,
                    Minutes = steps / 100.0,
                    Calories = MinutesToCalories(steps / 100.0),
                    HasData = has,
                    IsToday = day == today,
                    IsFuture = day > today
                });
            }
        }

        private void LoadMonth()
        {
            MonthDays.Clear();
            var history = _svc.StepHistory;
            var today = DateTime.UtcNow.Date;

            for (var d = MonthStart; d <= MonthEnd; d = d.AddDays(1))
            {
                var key = d.ToString("yyyy-MM-dd");
                bool has = history.ContainsKey(key) || d == today;
                history.TryGetValue(key, out var steps);

                MonthDays.Add(new DayStat
                {
                    Date = d,
                    Steps = steps,
                    DistanceKm = steps * _strideCm / 100000.0,
                    Minutes = steps / 100.0,
                    Calories = MinutesToCalories(steps / 100.0),
                    HasData = has,
                    IsToday = d == today,
                    IsFuture = d > today
                });
            }
        }

        private static int Sum(int[] hours) => hours?.Sum() ?? 0;

        private int GetDayTotalSteps()
        {
            var today = DateTime.UtcNow.Date;
            return AnchorDate == today ? _svc.Last24HoursSteps : Sum(HourlyStepsForDay);
        }

        // ---------- FIXED: all-time daily average ----------
        // Now starts from the first day that actually has recorded steps (>0).
        // If there’s no history, we use today (days = 1). Today’s live steps are
        // added if today isn’t already present in history.
        private int GetAllTimeDailyAverageSteps()
        {
            var history = _svc.StepHistory;
            var today = DateTime.UtcNow.Date;
            var todayKey = today.ToString("yyyy-MM-dd");

            // Find the first date that has >0 steps
            DateTime? firstWithSteps = null;
            foreach (var kvp in history)
            {
                if (kvp.Value <= 0) continue;
                if (DateTime.TryParseExact(kvp.Key, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                {
                    d = d.Date;
                    firstWithSteps = firstWithSteps.HasValue
                        ? (d < firstWithSteps.Value ? d : firstWithSteps)
                        : d;
                }
            }

            var startDate = firstWithSteps ?? today; // if no history with steps, start is today

            // Sum total (include today's live steps if not already stored)
            long total = history.Values.Select(v => (long)v).Sum();
            if (!history.ContainsKey(todayKey))
                total += _svc.Last24HoursSteps;

            int days = Math.Max(1, (int)(today - startDate).TotalDays + 1);
            return (int)Math.Round(total / (double)days, MidpointRounding.AwayFromZero);
        }

        private static DateTime GetWeekStartOf(DateTime date, bool mondayStart)
            => mondayStart
               ? date.AddDays(-(int)((((int)date.DayOfWeek + 6) % 7))) // Monday=0
               : date.AddDays(-(int)date.DayOfWeek);                   // Sunday=0

        private DateTime GetWeekStartOf(DateTime date) => GetWeekStartOf(date, _isMondayStart);

        private double MinutesToCalories(double minutes)
        {
            var kcalPerMin = 3.5 * 3.5 * _weightKg / 200.0;
            return minutes * kcalPerMin;
        }

        private void OnPropertyChanged(string n)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
