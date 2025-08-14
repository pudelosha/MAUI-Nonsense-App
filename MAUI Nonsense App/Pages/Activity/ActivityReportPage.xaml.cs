using System.ComponentModel;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Pages.Activity;

public partial class ActivityReportPage : ContentPage
{
    private readonly ActivityReportViewModel _vm;

    private readonly Pages._Drawable.WeeklyBarChartDrawable _weekly;
    private readonly Pages._Drawable.DailyHourlyChartDrawable _daily;
    private readonly Pages._Drawable.MonthlyBarChartDrawable _monthly;

    private bool _panHandled;
    private bool _isSliding;
    private const double PanThreshold = 40;

    // NEW: defaultRange parameter (defaults to Week)
    public ActivityReportPage(IStepCounterService stepService, ReportRange defaultRange = ReportRange.Week)
    {
        InitializeComponent();

        _vm = new ActivityReportViewModel(stepService);
        BindingContext = _vm;

        // Apply initial range BEFORE wiring drawables
        _vm.SelectedRange = defaultRange;

        _weekly = new Pages._Drawable.WeeklyBarChartDrawable(_vm);
        _daily = new Pages._Drawable.DailyHourlyChartDrawable(_vm);
        _monthly = new Pages._Drawable.MonthlyBarChartDrawable(_vm);

        AttachDrawableForRange();

        _vm.RedrawRequested += (_, __) =>
        {
            if (_isSliding) { Chart.Invalidate(); return; }
            AnimateBarsGrowth();
        };

        _vm.PropertyChanged += Vm_PropertyChanged;
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActivityReportViewModel.SelectedRange))
        {
            AttachDrawableForRange();
            AnimateBarsGrowth();
        }
        else if (e.PropertyName == nameof(ActivityReportViewModel.DailyGoalSteps))
        {
            Chart.Invalidate();
        }
    }

    private void AttachDrawableForRange()
    {
        Chart.Drawable = _vm.SelectedRange switch
        {
            ReportRange.Day => _daily,
            ReportRange.Month => _monthly,
            _ => _weekly
        };
        Chart.Invalidate();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ChartCard.SizeChanged += ChartCard_SizeChanged;
        ChartCard_SizeChanged(null, EventArgs.Empty);
        AnimateBarsGrowth();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ChartCard.SizeChanged -= ChartCard_SizeChanged;
        this.AbortAnimation("BarGrow");
    }

    private void ChartCard_SizeChanged(object? sender, EventArgs e)
    {
        var pad = ChartCard.Padding;
        var h = ChartCard.Height - pad.Top - pad.Bottom;
        if (h > 0) Chart.HeightRequest = h;
    }

    private async void Prev_Clicked(object sender, EventArgs e) => await SlideRangeAsync(-1);
    private async void Next_Clicked(object sender, EventArgs e) => await SlideRangeAsync(+1);

    private async void OnChartSwipedLeft(object sender, SwipedEventArgs e) => await SlideRangeAsync(+1);
    private async void OnChartSwipedRight(object sender, SwipedEventArgs e) => await SlideRangeAsync(-1);

    private async void OnChartPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                if (_panHandled) return;
                if (e.TotalX <= -PanThreshold) { _panHandled = true; await SlideRangeAsync(+1); }
                else if (e.TotalX >= PanThreshold) { _panHandled = true; await SlideRangeAsync(-1); }
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _panHandled = false;
                break;
        }
    }

    private async Task SlideRangeAsync(int dir)
    {
        if (dir > 0 && !_vm.CanGoForward) return;

        _isSliding = true;
        this.AbortAnimation("BarGrow");
        SetGrowth(1f);

        double w = Chart.Width > 0 ? Chart.Width : ChartCard.Width;
        if (w <= 0) w = 320;

        Chart.TranslationX = dir * w;
        _vm.TryShiftRange(dir);
        AttachDrawableForRange();
        await Chart.TranslateTo(0, 0, 250, Easing.CubicOut);

        _isSliding = false;
    }

    private void AnimateBarsGrowth()
    {
        this.AbortAnimation("BarGrow");
        SetGrowth(0f);
        Chart.Invalidate();

        var anim = new Animation(p =>
        {
            SetGrowth((float)p);
            Chart.Invalidate();
        }, 0, 1, Easing.SinOut);

        anim.Commit(this, "BarGrow", length: 900);
    }

    private void SetGrowth(float v)
    {
        switch (Chart.Drawable)
        {
            case Pages._Drawable.WeeklyBarChartDrawable w: w.GrowthProgress = v; break;
            case Pages._Drawable.DailyHourlyChartDrawable d: d.GrowthProgress = v; break;
            case Pages._Drawable.MonthlyBarChartDrawable m: m.GrowthProgress = v; break;
        }
    }
}
