using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Pages;

public partial class StepCounterPage : ContentPage
{
    private readonly IStepCounterService _stepService;

    public StepCounterPage(IStepCounterService stepService)
    {
        InitializeComponent();
        _stepService = stepService;

        // initialize UI with current values
        UpdateLabels();

        _stepService.StepsUpdated += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(UpdateLabels);
        };
    }

    private void UpdateLabels()
    {
        TotalStepsLabel.Text = $"Steps: {_stepService.TotalSteps}";
        Last24HoursLabel.Text = $"Last 24h: {_stepService.Last24HoursSteps}";
    }
}
