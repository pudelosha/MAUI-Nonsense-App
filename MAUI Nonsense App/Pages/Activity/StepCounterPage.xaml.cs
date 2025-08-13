using MAUI_Nonsense_App.Services;
using MAUI_Nonsense_App.ViewModels;
using MAUI_Nonsense_App.Pages._Drawable;

namespace MAUI_Nonsense_App.Pages.Activity;

public partial class StepCounterPage : ContentPage
{
    private readonly IStepCounterService _stepService;
    private readonly StepCounterViewModel _viewModel;
    private readonly System.Timers.Timer _refreshTimer;

    private readonly TodayHourlyChartDrawable _hourlyDrawable = new();

    public StepCounterPage(IStepCounterService stepService)
    {
        InitializeComponent();

        _stepService = stepService;
        _viewModel = new StepCounterViewModel(_stepService);
        BindingContext = _viewModel;

        var hv = this.FindByName<GraphicsView>("HourlyChart");
        if (hv != null) hv.Drawable = _hourlyDrawable;

        _refreshTimer = new System.Timers.Timer(5000) { AutoReset = true };
        _refreshTimer.Elapsed += (s, e) => RefreshStepData();

        Device.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            _stepService?.RaiseStepsUpdated();
            return false;
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshStepData();
        _refreshTimer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshTimer.Stop();
    }

    private async void OnOpenReport(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new ActivityReportPage(_stepService));
    }

    private void RefreshStepData()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _viewModel.TodaySteps = _stepService.Last24HoursSteps;
            _viewModel.ActiveSeconds = _stepService.ActiveSecondsToday; // not used in calc anymore, but kept
            _viewModel.ReloadLast7Days();

            var hv = this.FindByName<GraphicsView>("HourlyChart");
            if (hv != null)
            {
                _hourlyDrawable.Hours = GetHourlyStepsToday();
                _hourlyDrawable.GrowthProgress = 1f;
                hv.Invalidate();
            }
        });
    }

    private int[] GetHourlyStepsToday()
    {
        try
        {
            var mi = _stepService.GetType().GetMethod("GetHourlySteps", new[] { typeof(DateTime) });
            if (mi != null)
            {
                var result = mi.Invoke(_stepService, new object[] { DateTime.UtcNow.Date }) as int[];
                if (result != null && result.Length == 24) return result;
            }
        }
        catch { }
        return new int[24];
    }

    private async void OnResetTodayClicked(object? sender, EventArgs e)
    {
        bool ok = await DisplayAlert("Reset today's data",
            "This will clear only today's counters and restart counting from now. Continue?",
            "Reset", "Cancel");

        if (!ok) return;

        _stepService.ResetToday();
        RefreshStepData();
    }

    private async void OnResetAllClicked(object? sender, EventArgs e)
    {
        bool ok = await DisplayAlert("Reset all data",
            "This will erase ALL accumulated history and counters. This cannot be undone. Continue?",
            "Erase all", "Cancel");

        if (!ok) return;

        _stepService.ResetAll();
        RefreshStepData();
    }
}
