using MAUI_Nonsense_App.Services;
using MAUI_Nonsense_App.ViewModels;
using System.Timers;

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

        _refreshTimer = new System.Timers.Timer(5000); // 5-second interval
        _refreshTimer.Elapsed += (s, e) => RefreshStepData();
        _refreshTimer.AutoReset = true;

        // Force one-time refresh to ensure UI shows correct numbers on startup
        Device.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            _stepService?.RaiseStepsUpdated(); // This triggers ViewModel update
            return false; // run once
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        RefreshStepData();      // Initial refresh
        _refreshTimer.Start();  // Start periodic refresh
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshTimer.Stop();   // Stop refresh when not visible
    }

    private void RefreshStepData()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Console.WriteLine($"[Page] RefreshStepData: Total={_stepService.TotalSteps}, Daily={_stepService.Last24HoursSteps}");
            _viewModel.TotalSteps = _stepService.TotalSteps;
            _viewModel.Last24HoursSteps = _stepService.Last24HoursSteps;
            _viewModel.ReloadLast7Days();
        });
    }
}
