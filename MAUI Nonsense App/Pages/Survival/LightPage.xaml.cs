using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;
using Microsoft.Maui.ApplicationModel;

namespace MAUI_Nonsense_App.Pages.Survival;

public partial class LightPage : ContentPage
{
    private readonly LightViewModel _vm;
    private CancellationTokenSource? _policeCts;

    public LightPage(ILightService lightService)
    {
        InitializeComponent();

        _vm = new LightViewModel(lightService);
        BindingContext = _vm;

        _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private async void OnToggleLightClicked(object sender, EventArgs e) =>
        await _vm.ToggleLightAsync();

    private async void OnToggleLighthouseClicked(object sender, EventArgs e) =>
        await _vm.ToggleLighthouseAsync();

    private async void OnToggleStrobeClicked(object sender, EventArgs e) =>
        await _vm.ToggleStrobeAsync();

    private async void OnToggleSOSClicked(object sender, EventArgs e) =>
        await _vm.ToggleSOSAsync();

    private async void OnTogglePoliceClicked(object sender, EventArgs e) =>
        await _vm.TogglePoliceAsync();

    private async void OnSendMorseClicked(object sender, EventArgs e)
    {
        var modal = new SendMorseModal(_vm);
        await Navigation.PushModalAsync(modal);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_vm.IsPoliceOn))
        {
            if (_vm.IsPoliceOn)
            {
                StartPoliceEffect();
            }
            else
            {
                StopPoliceEffect();
            }
        }
    }

    private void StartPoliceEffect()
    {
        _policeCts?.Cancel();
        _policeCts = new CancellationTokenSource();
        var token = _policeCts.Token;

        _ = Task.Run(async () =>
        {
            Color[] colors = [Colors.White, Colors.Blue, Colors.White, Colors.Red];
            int index = 0;

            while (!token.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    this.BackgroundColor = colors[index];
                });

                index = (index + 1) % colors.Length;

                try
                {
                    await Task.Delay(500, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                this.BackgroundColor = Colors.White;
            });
        });
    }

    private void StopPoliceEffect()
    {
        _policeCts?.Cancel();
        _policeCts = null;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            this.BackgroundColor = Colors.White;
        });
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        // stop all effects and release torch on exit
        await _vm.TurnOffAllAsync();

        StopPoliceEffect();
    }
}
