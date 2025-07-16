using MAUI_Nonsense_App.Services;
using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages.Activity;

public partial class StepCounterPage : ContentPage
{
    public StepCounterPage(IStepCounterService stepService)
    {
        InitializeComponent();

        BindingContext = new StepCounterViewModel(stepService);
    }
}
