using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages.Activity;

public partial class StepCounterPage : ContentPage
{
    private readonly StepCounterViewModel _vm;

    public StepCounterPage(IStepCounterService stepService)
    {
        InitializeComponent();
        _vm = new StepCounterViewModel(stepService);
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.StartTimer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.StopTimer();
    }
}
