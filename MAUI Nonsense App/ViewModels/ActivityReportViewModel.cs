using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Pages.Activity;

public enum MetricMode { Steps, Calories, Time, Distance }

public class DayStat
{
    public DateTime Date { get; set; }
    public string Abbrev => Date.ToString("ddd", CultureInfo.InvariantCulture);
    public int Steps { get; set; }
    public double DistanceKm { get; set; }   // derived
    public double Calories { get; set; }     // derived
    public double Minutes { get; set; }      // derived
    public bool IsToday { get; set; }
    public bool IsFuture { get; set; }       // used to hide future labels
}

public class ActivityReportViewModel : INotifyPropertyChanged
{
    private readonly IStepCounterService _service;

    // preference keys (match SettingsViewModel)
    private const string KeyWeekStart = "Settings.WeekStart";     // "Monday" | "Sunday"
    private const string KeyDailyGoal = "Settings.DailyGoal";
    private const string KeyStrideLengthCm = "Settings.StrideLengthCm";
    private const string KeyWeightKg = "Settings.WeightKg";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? RedrawRequested;

    public ObservableCollection<DayStat> Days { get; } = new();

    // week window
    public DateTime WeekStart { get; private set; }
    public DateTime WeekEnd => WeekStart.AddDays(6);

    // header: today’s steps (centered)
    public string TodayStepsText => _service.Last24HoursSteps.ToString("N0");

    // header: average for selected week so far (steps only)
    public string DailyAverageSoFarText
    {
        get
        {
            var today = DateTime.UtcNow.Date;
            // end day for averaging: if current week -> today; if past week -> week end; if future (blocked) -> none
            var end = WeekEnd <= today ? WeekEnd : (today < WeekStart ? WeekStart.AddDays(-1) : today);

            int daysCount = 0;
            int sum = 0;
            foreach (var d in Days)
            {
                if (d.Date >= WeekStart && d.Date <= end)
                {
                    daysCount++;
                    sum += d.Steps;
                }
            }
            if (daysCount <= 0) return "0";
            return (sum / (double)daysCount).ToString("N0");
        }
    }

    public string WeekRangeText => $"{WeekStart:dd.MM} - {WeekEnd:dd.MM}";

    // navigate enablement (block moving into future)
    public bool CanGoForward
    {
        get
        {
            var currentStart = GetWeekStartOf(DateTime.UtcNow.Date);
            return WeekStart < currentStart;
        }
    }

    // metric selection (used by chart; header stays in steps)
    private MetricMode _selectedMode = MetricMode.Steps;
    public MetricMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (_selectedMode == value) return;
            _selectedMode = value;
            NotifyModeChanged();
        }
    }

    public bool StepsSelected => SelectedMode == MetricMode.Steps;
    public bool CaloriesSelected => SelectedMode == MetricMode.Calories;
    public bool TimeSelected => SelectedMode == MetricMode.Time;
    public bool DistanceSelected => SelectedMode == MetricMode.Distance;

    public IRelayCommand SetModeCommand { get; }

    // weekly totals (if needed elsewhere)
    public int WeeklySumSteps => Days.Sum(d => d.Steps);
    public double WeeklySumDistanceKm => Days.Sum(d => d.DistanceKm);
    public double WeeklySumMinutes => Days.Sum(d => d.Minutes);
    public double WeeklySumCalories => Days.Sum(d => d.Calories);

    // conversions / goals
    private double _strideCm;
    private double _weightKg;

    public int DailyGoalSteps { get; private set; }
    public double GoalDistanceKm => DailyGoalSteps * _strideCm / 100_000.0;
    public double GoalMinutes => DailyGoalSteps / 100.0;               // 100 steps/min
    public double GoalCalories => MinutesToCalories(GoalMinutes);

    // week-start rule (true => Monday-first)
    private readonly bool _isMondayStart;

    public ActivityReportViewModel(IStepCounterService service)
    {
        _service = service;

        _isMondayStart = Preferences.Get(KeyWeekStart, "Monday")
                                    .Equals("Monday", StringComparison.OrdinalIgnoreCase);

        var today = DateTime.UtcNow.Date;
        WeekStart = GetWeekStartOf(today);

        DailyGoalSteps = Preferences.Get(KeyDailyGoal, 10000);
        _strideCm = Preferences.ContainsKey(KeyStrideLengthCm) ? Preferences.Get(KeyStrideLengthCm, 75) : 75;
        _weightKg = Preferences.ContainsKey(KeyWeightKg) ? Preferences.Get(KeyWeightKg, 70.0) : 70.0;

        SetModeCommand = new RelayCommand<string>(s =>
        {
            if (Enum.TryParse<MetricMode>(s, true, out var m))
                SelectedMode = m;
        });

        // live updates -> keep header & chart in sync when showing current week
        _service.StepsUpdated += (_, __) =>
        {
            if (DateTime.UtcNow.Date >= WeekStart && DateTime.UtcNow.Date <= WeekEnd)
                LoadWeek();
            OnPropertyChanged(nameof(TodayStepsText));
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

    private void NotifyModeChanged()
    {
        OnPropertyChanged(nameof(SelectedMode));
        OnPropertyChanged(nameof(StepsSelected));
        OnPropertyChanged(nameof(CaloriesSelected));
        OnPropertyChanged(nameof(TimeSelected));
        OnPropertyChanged(nameof(DistanceSelected));
        RedrawRequested?.Invoke(this, EventArgs.Empty);
    }

    private void LoadWeek()
    {
        Days.Clear();
        var history = _service.StepHistory; // date "yyyy-MM-dd" -> steps
        var today = DateTime.UtcNow.Date;

        for (int i = 0; i < 7; i++)
        {
            var day = WeekStart.AddDays(i);
            history.TryGetValue(day.ToString("yyyy-MM-dd"), out var steps);

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
                IsFuture = day > today
            });
        }

        OnPropertyChanged(nameof(WeekRangeText));
        OnPropertyChanged(nameof(DailyAverageSoFarText));
        OnPropertyChanged(nameof(CanGoForward));
        RedrawRequested?.Invoke(this, EventArgs.Empty);
    }

    private double MinutesToCalories(double minutes)
    {
        // Walking easy MET ~ 3.5; kcal/min = MET * 3.5 * kg / 200
        var kcalPerMin = 3.5 * 3.5 * _weightKg / 200.0;
        return minutes * kcalPerMin;
    }

    protected void OnPropertyChanged(string n)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
