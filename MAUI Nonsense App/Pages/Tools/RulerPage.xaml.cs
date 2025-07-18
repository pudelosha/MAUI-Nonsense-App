using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Pages.Tools;

public partial class RulerPage : ContentPage
{
    private readonly RulerViewModel _vm;

    public RulerPage(IScreenMetricsService screenMetricsService)
    {
        InitializeComponent();
        _vm = new RulerViewModel(screenMetricsService);
        BindingContext = _vm;

        LeftRuler.Drawable = new RulerDrawable(_vm, isRight: false);
        RightRuler.Drawable = new RulerDrawable(_vm, isRight: true);

        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_vm.DipsPerUnit) || e.PropertyName == nameof(_vm.UnitLabel))
            {
                LeftRuler.Invalidate();
                RightRuler.Invalidate();
            }
        };

        Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
        {
            LeftRuler.Invalidate();
            RightRuler.Invalidate();
            return true;
        });
    }
}

