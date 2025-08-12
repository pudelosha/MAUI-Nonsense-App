using MAUI_Nonsense_App.Pages._Drawable;
using MAUI_Nonsense_App.ViewModels;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages.Games;

public partial class _2048Page : ContentPage
{
    private readonly Game2048ViewModel _vm;
    private readonly Game2048Drawable _drawable;

    public _2048Page()
    {
        InitializeComponent();

        _vm = new Game2048ViewModel();
        _drawable = new Game2048Drawable(_vm);
        GameCanvas.Drawable = _drawable;
        BindingContext = _vm;

        GameCanvas.SizeChanged += (s, e) =>
        {
            var size = new Size(GameCanvas.Width, GameCanvas.Height);
            _vm.SetCanvasSize(size);
        };

        _vm.GameOverEvent += async finalScore =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayAlert("Game Over", $"Final score: {finalScore}", "OK"));
            _vm.Ready(); // back to Ready; Start or first swipe will begin again
            GameCanvas.Invalidate();
        };
    }

    // Swipe gestures
    private void OnSwipe(object sender, SwipedEventArgs e)
    {
        switch (e.Direction)
        {
            case SwipeDirection.Up: _vm.Move(MoveDir.Up); break;
            case SwipeDirection.Down: _vm.Move(MoveDir.Down); break;
            case SwipeDirection.Left: _vm.Move(MoveDir.Left); break;
            case SwipeDirection.Right: _vm.Move(MoveDir.Right); break;
        }
    }

    // Buttons
    private void OnStartClicked(object sender, EventArgs e) => _vm.Start(GameCanvas);
    private void OnUpClicked(object sender, EventArgs e) => _vm.Move(MoveDir.Up);
    private void OnDownClicked(object sender, EventArgs e) => _vm.Move(MoveDir.Down);
    private void OnLeftClicked(object sender, EventArgs e) => _vm.Move(MoveDir.Left);
    private void OnRightClicked(object sender, EventArgs e) => _vm.Move(MoveDir.Right);
}
