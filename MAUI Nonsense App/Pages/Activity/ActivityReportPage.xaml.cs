using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Pages.Activity;

public partial class ActivityReportPage : ContentPage
{
    private readonly ActivityReportViewModel _vm;
    private readonly Pages._Drawable.WeeklyBarChartDrawable _drawable;

    private bool _panHandled;
    private bool _isSliding;
    private const double PanThreshold = 40;

    public ActivityReportPage(IStepCounterService stepService)
    {
        InitializeComponent();
        _vm = new ActivityReportViewModel(stepService);
        BindingContext = _vm;

        _drawable = new Pages._Drawable.WeeklyBarChartDrawable(_vm);
        Chart.Drawable = _drawable;

        // Animate bars when the VM asks us to redraw and we're not sliding weeks
        _vm.RedrawRequested += (_, __) =>
        {
            if (_isSliding) { Chart.Invalidate(); return; }
            AnimateBarsGrowth();
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ChartCard.SizeChanged += ChartCard_SizeChanged;
        ChartCard_SizeChanged(null, EventArgs.Empty); // force once
        AnimateBarsGrowth(); // initial grow
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ChartCard.SizeChanged -= ChartCard_SizeChanged;
        this.AbortAnimation("BarGrow");
    }

    // GraphicsView fills the frame
    private void ChartCard_SizeChanged(object? sender, EventArgs e)
    {
        var pad = ChartCard.Padding;
        var h = ChartCard.Height - pad.Top - pad.Bottom;
        if (h > 0) Chart.HeightRequest = h;
    }

    // Header arrows -> slide animation
    private async void PrevWeek_Clicked(object sender, EventArgs e) => await SlideWeekAsync(-1);
    private async void NextWeek_Clicked(object sender, EventArgs e) => await SlideWeekAsync(+1);

    // Swipe gesture -> slide
    private async void OnChartSwipedLeft(object sender, SwipedEventArgs e) => await SlideWeekAsync(+1);
    private async void OnChartSwipedRight(object sender, SwipedEventArgs e) => await SlideWeekAsync(-1);

    // Pan fallback if Swipe doesn't fire on some devices
    private async void OnChartPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                if (_panHandled) return;
                if (e.TotalX <= -PanThreshold) { _panHandled = true; await SlideWeekAsync(+1); }
                else if (e.TotalX >= PanThreshold) { _panHandled = true; await SlideWeekAsync(-1); }
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _panHandled = false;
                break;
        }
    }

    // Slide the chart in from the side when changing weeks
    private async Task SlideWeekAsync(int dir)
    {
        if (dir > 0 && !_vm.CanGoForward) return;

        _isSliding = true;
        this.AbortAnimation("BarGrow"); // don't grow while sliding
        _drawable.GrowthProgress = 1f;

        double w = Chart.Width > 0 ? Chart.Width : ChartCard.Width;
        if (w <= 0) w = 320;

        // place new content off-screen, update VM, then animate to 0
        Chart.TranslationX = dir * w;
        _vm.TryShiftWeek(dir);
        Chart.Invalidate();
        await Chart.TranslateTo(0, 0, 250, Easing.CubicOut);

        _isSliding = false;
    }

    // Grow bars from bottom for mode changes
    private void AnimateBarsGrowth()
    {
        this.AbortAnimation("BarGrow");
        _drawable.GrowthProgress = 0f;
        Chart.Invalidate();

        var anim = new Animation(p =>
        {
            _drawable.GrowthProgress = (float)p;
            Chart.Invalidate();
        }, 0, 1, Easing.SinOut); // gentler than CubicOut

        const uint BarGrowMs = 900;   // <-- slower (was 350)
        anim.Commit(this, "BarGrow", length: BarGrowMs);
    }
}
