using MAUI_Nonsense_App.Pages._Drawable;
using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages.Games;

public partial class SpaceInvadersPage : ContentPage
{
    private readonly SpaceInvadersViewModel _vm;
    private readonly SpaceInvadersDrawable _drawable;
    private double _panLastX;

    public SpaceInvadersPage()
    {
        InitializeComponent();

        _vm = new SpaceInvadersViewModel();
        _drawable = new SpaceInvadersDrawable(_vm);
        GameCanvas.Drawable = _drawable;
        BindingContext = _vm;

        GameCanvas.SizeChanged += (s, e) =>
        {
            var size = new Size(GameCanvas.Width, GameCanvas.Height);
            _vm.SetCanvasSize(size);
        };

        _vm.GameOverEvent += async (score, wave) =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayAlert("Game Over", $"Final score: {score}\nWave: {wave}", "OK"));
            _vm.Ready(); // back to Ready; Start or input to play again
            GameCanvas.Invalidate();
        };
    }

    private void OnStart(object sender, EventArgs e) => _vm.Start(GameCanvas);
    private void OnPause(object sender, EventArgs e) => _vm.Pause();
    private void OnLeft(object sender, EventArgs e) { StartOrResumeIfNeeded(); _vm.Nudge(-1); }
    private void OnRight(object sender, EventArgs e) { StartOrResumeIfNeeded(); _vm.Nudge(1); }
    private void OnFire(object sender, EventArgs e) { StartOrResumeIfNeeded(); _vm.Fire(); }

    private void StartOrResumeIfNeeded()
    {
        if (_vm.State == SIState.Ready || _vm.State == SIState.GameOver)
            _vm.Start(GameCanvas);
        else if (_vm.State == SIState.Paused)
            _vm.Resume();
    }

    // Pan to move (also starts/resumes)
    private void OnPan(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Started)
        {
            _panLastX = e.TotalX;
            StartOrResumeIfNeeded();
        }
        else if (e.StatusType == GestureStatus.Running)
        {
            StartOrResumeIfNeeded();
            var dx = e.TotalX - _panLastX;
            _panLastX = e.TotalX;
            _vm.MoveBy((float)dx);
        }
    }

    // Tap to fire (also starts/resumes)
    private void OnTap(object? sender, TappedEventArgs e)
    {
        StartOrResumeIfNeeded();
        _vm.Fire();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Pause();
    }
}
