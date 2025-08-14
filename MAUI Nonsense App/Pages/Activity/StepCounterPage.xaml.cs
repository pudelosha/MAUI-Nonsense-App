using MAUI_Nonsense_App.Services;
using MAUI_Nonsense_App.ViewModels;
using MAUI_Nonsense_App.Pages.Activity;

namespace MAUI_Nonsense_App.Pages.Activity;

public partial class StepCounterPage : ContentPage
{
    private readonly IStepCounterService _stepService;
    private readonly StepCounterViewModel _viewModel;
    private readonly System.Timers.Timer _refreshTimer;

    public StepCounterPage(IStepCounterService stepService)
    {
        InitializeComponent();

        _stepService = stepService;
        _viewModel = new StepCounterViewModel(_stepService);
        BindingContext = _viewModel;

        // Refresh small UI bits periodically
        _refreshTimer = new System.Timers.Timer(5000) { AutoReset = true };
        _refreshTimer.Elapsed += (s, e) => RefreshStepData();

        // nudge the service once after opening
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

    private void RefreshStepData()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _viewModel.TodaySteps = _stepService.Last24HoursSteps;
            _viewModel.ActiveSeconds = _stepService.ActiveSecondsToday;
            _viewModel.ReloadLast7Days();
        });
    }

    // --- Navigation to report with default range ---
    private async void OnOpenReportDaily(object? sender, TappedEventArgs e)
        => await Navigation.PushAsync(new ActivityReportPage(_stepService, ReportRange.Day));

    private async void OnOpenReportWeekly(object? sender, TappedEventArgs e)
        => await Navigation.PushAsync(new ActivityReportPage(_stepService, ReportRange.Week));

    private async void OnOpenReportMonthly(object? sender, TappedEventArgs e)
        => await Navigation.PushAsync(new ActivityReportPage(_stepService, ReportRange.Month));

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
