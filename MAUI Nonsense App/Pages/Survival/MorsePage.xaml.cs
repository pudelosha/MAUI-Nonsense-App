using MAUI_Nonsense_App.Services;
using MAUI_Nonsense_App.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace MAUI_Nonsense_App.Pages.Survival;

public partial class MorsePage : ContentPage
{
    private readonly MorseViewModel _vm;

    public MorsePage(ILightService lightService)
    {
        InitializeComponent();

        _vm = new MorseViewModel(lightService);

        // Preview text from VM
        _vm.PreviewChanged += s =>
            MainThread.BeginInvokeOnMainThread(() => PreviewLabel.Text = s);

        // Screen flash callback (KEEP INSTANT for clarity)
        _vm.ScreenFlashChanged += on =>
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (on)
                {
                    ScreenFlash.IsVisible = true;
                    ScreenFlash.Opacity = 1; // immediate
                    // await ScreenFlash.FadeTo(1, 0); // optional
                }
                else
                {
                    ScreenFlash.Opacity = 0; // immediate
                    ScreenFlash.IsVisible = false;
                    // await ScreenFlash.FadeTo(0, 0);
                }
            });

        // initial state
        _vm.UpdatePreview(MessageEntry.Text ?? string.Empty);
        SpeedLabel.Text = $"Speed: {(int)SpeedSlider.Value} WPM";
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Stop();
        ScreenFlash.IsVisible = false;
        ScreenFlash.Opacity = 0;
    }

    private void OnMessageChanged(object sender, TextChangedEventArgs e)
        => _vm.UpdatePreview(e.NewTextValue ?? string.Empty);

    private void OnSpeedChanged(object sender, ValueChangedEventArgs e)
    {
        _vm.Wpm = (int)Math.Round(e.NewValue);
        SpeedLabel.Text = $"Speed: {_vm.Wpm} WPM";
    }

    private void OnFillSOS(object sender, EventArgs e)
    {
        MessageEntry.Text = "SOS";
        _vm.UpdatePreview("SOS");
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        SendBtn.IsEnabled = false;
        try
        {
            await _vm.PlayAsync(MessageEntry.Text ?? string.Empty);
        }
        finally
        {
            SendBtn.IsEnabled = true;
        }
    }

    private void OnStopClicked(object sender, EventArgs e) => _vm.Stop();
}
