using MAUI_Nonsense_App.Services;
using MAUI_Nonsense_App.ViewModels;

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

        _refreshTimer = new System.Timers.Timer(5000);
        _refreshTimer.Elapsed += (s, e) => RefreshStepData();
        _refreshTimer.AutoReset = true;

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
            _viewModel.ActiveSeconds = _stepService.ActiveSecondsToday;
            _viewModel.ReloadLast7Days();
        });
    }
}
