using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages.Survival;

public partial class MovementDetectorPage : ContentPage
{
    private readonly MovementDetectorViewModel _viewModel;

    public MovementDetectorPage(MovementDetectorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}