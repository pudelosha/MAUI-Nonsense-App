using MAUI_Nonsense_App.Pages._Drawable;
using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages.Games;

public partial class PongGame : ContentPage
{
    private readonly PongViewModel _vm;
    private readonly PongDrawable _drawable;
    private double _panLastY;

    public PongGame()
    {
        InitializeComponent();

        _vm = new PongViewModel();
        _drawable = new PongDrawable(_vm);
        PongCanvas.Drawable = _drawable;
        BindingContext = _vm;

        PongCanvas.SizeChanged += (s, e) =>
        {
            var size = new Size(PongCanvas.Width, PongCanvas.Height);
            _vm.SetCanvasSize(size);
        };

        _vm.GameOverEvent += async msg =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayAlert("Pong", msg, "OK"));
            _vm.Ready();         // back to Ready; Start or input to begin again
            PongCanvas.Invalidate();
        };
    }

    private void OnUp(object sender, EventArgs e)
    {
        StartOrResumeIfNeeded();
        _vm.NudgePlayer(-1);
    }

    private void OnDown(object sender, EventArgs e)
    {
        StartOrResumeIfNeeded();
        _vm.NudgePlayer(1);
    }

    private void OnPause(object sender, EventArgs e) => _vm.Pause();
    private void OnStart(object sender, EventArgs e) => _vm.Start(PongCanvas);

    private void StartOrResumeIfNeeded()
    {
        if (_vm.State == PongState.Ready || _vm.State == PongState.RoundPaused || _vm.State == PongState.GameOver)
            _vm.StartOrResume(PongCanvas);
    }

    // Drag to move paddle (also starts/resumes)
    private void OnPan(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Started)
        {
            _panLastY = e.TotalY;
            StartOrResumeIfNeeded();
        }
        else if (e.StatusType == GestureStatus.Running)
        {
            StartOrResumeIfNeeded();
            var dy = e.TotalY - _panLastY;
            _panLastY = e.TotalY;
            _vm.MovePlayerBy((float)dy);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Pause();
    }
}
