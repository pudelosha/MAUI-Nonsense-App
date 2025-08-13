using MAUI_Nonsense_App.ViewModels;
using MAUI_Nonsense_App.Pages._Drawable;

namespace MAUI_Nonsense_App.Pages.Random;

public partial class RouletteWheelPage : ContentPage
{
    private readonly RouletteViewModel _viewModel;
    private readonly RouletteDrawable _drawable;

    public RouletteWheelPage()
    {
        InitializeComponent();
        _viewModel = new RouletteViewModel();
        _drawable = new RouletteDrawable(_viewModel);
        WheelCanvas.Drawable = _drawable;
        BindingContext = _viewModel;

        // Ensure initial label
        ResultLabel.Text = "Result: -";
    }

    private async void OnSpinClicked(object sender, EventArgs e)
    {
        await _viewModel.SpinBall(WheelCanvas);
        ResultLabel.Text = $"Result: {_viewModel.SelectedSlot}";
    }

    private void OnLayoutToggled(object sender, ToggledEventArgs e)
    {
        _viewModel.IsAmerican = e.Value;
        _viewModel.ResetWheel();
        WheelCanvas.Invalidate();
    }
}
