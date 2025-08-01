using Microsoft.Maui.Graphics;
using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages.Tools;

public partial class LevelPage : ContentPage
{
    private readonly LevelViewModel _viewModel;

    public LevelPage(LevelViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        BindingContext = _viewModel;

        _viewModel.PropertyChanged += (_, __) => UpdateUI();

        // Wire up drawables that read _viewModel values
        LevelCanvas.Drawable = new LevelDrawable(() => ((float)_viewModel.Roll / 90f, (float)_viewModel.Pitch / 90f));
        HorizontalBubbleCanvas.Drawable = new HorizontalBubbleDrawable(() => (float)_viewModel.Roll / 90f);
        VerticalBubbleCanvas.Drawable = new VerticalBubbleDrawable(() => (float)_viewModel.Pitch / 90f);

        Device.StartTimer(TimeSpan.FromMilliseconds(30), () =>
        {
            LevelCanvas.Invalidate();
            HorizontalBubbleCanvas.Invalidate();
            VerticalBubbleCanvas.Invalidate();
            return true;
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Start the sensor service when the page appears
        await _viewModel.StartAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        // Stop the sensor service when the page disappears
        await _viewModel.StopAsync();
    }

    private void UpdateUI()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ReadingLabel.Text = $"X: {_viewModel.Roll:F1}°   Y: {_viewModel.Pitch:F1}°";
        });
    }
}
