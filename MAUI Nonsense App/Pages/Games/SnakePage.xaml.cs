using MAUI_Nonsense_App.Pages._Drawable;
using MAUI_Nonsense_App.ViewModels;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages.Games;

public partial class SnakePage : ContentPage
{
    private readonly SnakeViewModel _viewModel;
    private readonly SnakeDrawable _drawable;

    public SnakePage()
    {
        InitializeComponent();

        _viewModel = new SnakeViewModel();
        _drawable = new SnakeDrawable(_viewModel);
        SnakeCanvas.Drawable = _drawable;
        BindingContext = _viewModel;

        SnakeCanvas.SizeChanged += (s, e) =>
        {
            var size = new Size(SnakeCanvas.Width, SnakeCanvas.Height);
            _viewModel.SetCanvasSize(size);
        };

        // Subscribe for game over popup + reset
        _viewModel.GameOverEvent += async finalScore =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayAlert("Game Over", $"Your score: {finalScore}", "OK"));
            _viewModel.Reset();
            SnakeCanvas.Invalidate();
        };
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Pause();
    }

    private void OnTurnLeftClicked(object sender, EventArgs e) => _viewModel.TurnLeft();
    private void OnTurnRightClicked(object sender, EventArgs e) => _viewModel.TurnRight();
    private void OnStartClicked(object sender, EventArgs e) => _viewModel.Start(SnakeCanvas);
    private void OnPauseClicked(object sender, EventArgs e) => _viewModel.Pause();
    private void OnResetClicked(object sender, EventArgs e)
    {
        _viewModel.Reset();
        SnakeCanvas.Invalidate();
    }

    private void OnSwipe(object sender, SwipedEventArgs e)
    {
        if (e.Direction == SwipeDirection.Left) _viewModel.TurnLeft();
        if (e.Direction == SwipeDirection.Right) _viewModel.TurnRight();
    }
}
