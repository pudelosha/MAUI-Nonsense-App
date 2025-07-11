using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Pages;

public partial class LightPage : ContentPage
{
    private readonly LightViewModel _vm;

    public LightPage(ILightService lightService)
    {
        InitializeComponent();

        _vm = new LightViewModel(lightService);
        BindingContext = _vm;
    }

    private async void OnToggleLightClicked(object sender, EventArgs e) =>
        await _vm.ToggleLightAsync();

    private async void OnToggleStrobeClicked(object sender, EventArgs e) =>
        await _vm.ToggleStrobeAsync();

    private async void OnToggleSOSClicked(object sender, EventArgs e) =>
        await _vm.ToggleSOSAsync();
}
