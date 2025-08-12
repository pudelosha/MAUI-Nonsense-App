using MAUI_Nonsense_App.Pages._Drawable;
using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages.Games;

public partial class TetrisPage : ContentPage
{
    private readonly TetrisViewModel _viewModel;
    private readonly TetrisDrawable _drawable;

    public TetrisPage()
    {
        InitializeComponent();

        _viewModel = new TetrisViewModel();
        _drawable = new TetrisDrawable(_viewModel);
        TetrisCanvas.Drawable = _drawable;
        BindingContext = _viewModel;

        TetrisCanvas.SizeChanged += (s, e) =>
        {
            var size = new Size(TetrisCanvas.Width, TetrisCanvas.Height);
            _viewModel.SetCanvasSize(size);
        };

        _viewModel.GameOverEvent += async finalScore =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayAlert("Game Over", $"Final score: {finalScore}", "OK"));
            _viewModel.Reset();          // back to Ready; press Start to begin again
            TetrisCanvas.Invalidate();
        };
    }

    private void OnMoveLeftClicked(object sender, EventArgs e) => _viewModel.MoveLeft();
    private void OnMoveRightClicked(object sender, EventArgs e) => _viewModel.MoveRight();
    private void OnRotateClicked(object sender, EventArgs e) => _viewModel.Rotate();
    private void OnDropClicked(object sender, EventArgs e) => _viewModel.Drop();
    private void OnPauseClicked(object sender, EventArgs e) => _viewModel.Pause();
    private void OnStartClicked(object sender, EventArgs e) => _viewModel.Start(TetrisCanvas);

    private void OnSwipe(object sender, SwipedEventArgs e)
    {
        if (e.Direction == SwipeDirection.Left) _viewModel.MoveLeft();
        if (e.Direction == SwipeDirection.Right) _viewModel.MoveRight();
        if (e.Direction == SwipeDirection.Down) _viewModel.Drop();
    }
}
