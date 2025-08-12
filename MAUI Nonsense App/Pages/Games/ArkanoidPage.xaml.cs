using MAUI_Nonsense_App.Pages._Drawable;
using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages.Games;

public partial class ArkanoidPage : ContentPage
{
    private readonly ArkanoidViewModel _vm;
    private readonly ArkanoidDrawable _drawable;

    private double _panLastX;

    public ArkanoidPage()
    {
        InitializeComponent();

        _vm = new ArkanoidViewModel();
        _drawable = new ArkanoidDrawable(_vm);
        ArkCanvas.Drawable = _drawable;
        BindingContext = _vm;

        ArkCanvas.SizeChanged += (s, e) =>
        {
            var size = new Size(ArkCanvas.Width, ArkCanvas.Height);
            _vm.SetCanvasSize(size);
        };

        _vm.GameOverEvent += async finalScore =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayAlert("Game Over", $"Final score: {finalScore}", "OK"));
            _vm.Reset(); // back to Ready; touch/Start/Left/Right will begin again
            ArkCanvas.Invalidate();
        };
    }

    // Buttons
    private void OnLeft(object sender, EventArgs e)
    {
        StartOrResumeIfNeeded();
        _vm.NudgePaddle(-1);
    }

    private void OnRight(object sender, EventArgs e)
    {
        StartOrResumeIfNeeded();
        _vm.NudgePaddle(1);
    }

    private void OnPause(object sender, EventArgs e) => _vm.Pause();
    private void OnStart(object sender, EventArgs e) => _vm.Start(ArkCanvas);

    private void StartOrResumeIfNeeded()
    {
        if (_vm.State == ArkanoidState.Ready || _vm.State == ArkanoidState.GameOver)
            _vm.Start(ArkCanvas);
        else if (_vm.State == ArkanoidState.Paused)
            _vm.Resume();
    }

    // Drag to move paddle (also starts/resumes the game)
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
            _vm.MovePaddleBy((float)dx);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Pause();
    }
}
