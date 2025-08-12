using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Pages.Activity;

public partial class ActivityReportPage : ContentPage
{
    private readonly ActivityReportViewModel _vm;
    private readonly Pages._Drawable.WeeklyBarChartDrawable _drawable;

    public ActivityReportPage(IStepCounterService stepService)
    {
        InitializeComponent();
        _vm = new ActivityReportViewModel(stepService);
        BindingContext = _vm;

        _drawable = new Pages._Drawable.WeeklyBarChartDrawable(_vm);
        Chart.Drawable = _drawable;

        _vm.RedrawRequested += (_, __) => Chart.Invalidate();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ChartCard.SizeChanged += ChartCard_SizeChanged;
        ChartCard_SizeChanged(null, EventArgs.Empty); // force once
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ChartCard.SizeChanged -= ChartCard_SizeChanged;
    }

    // Make GraphicsView fill the frame's inner height
    private void ChartCard_SizeChanged(object? sender, EventArgs e)
    {
        var pad = ChartCard.Padding;
        var h = ChartCard.Height - pad.Top - pad.Bottom;
        if (h > 0) Chart.HeightRequest = h;
    }

    // Header arrows
    private void PrevWeek_Clicked(object sender, EventArgs e) => _vm.TryShiftWeek(-1);
    private void NextWeek_Clicked(object sender, EventArgs e) => _vm.TryShiftWeek(+1);

    // Swipe & pan on the chart itself -> change weeks
    private void OnChartSwipedLeft(object sender, SwipedEventArgs e) => _vm.TryShiftWeek(+1);
    private void OnChartSwipedRight(object sender, SwipedEventArgs e) => _vm.TryShiftWeek(-1);

    // Pan fallback (threshold) in case Swipe doesn't fire on some devices
    private bool _panHandled;
    private const double PanThreshold = 40;

    private void OnChartPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                if (_panHandled) return;
                if (e.TotalX <= -PanThreshold) { _vm.TryShiftWeek(+1); _panHandled = true; }
                else if (e.TotalX >= PanThreshold) { _vm.TryShiftWeek(-1); _panHandled = true; }
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _panHandled = false;
                break;
        }
    }
}
