using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages.Tools;

public partial class ProtractorPage : ContentPage
{
    private readonly ProtractorViewModel _vm;
    private int? _activeLine;

    public ProtractorPage()
    {
        InitializeComponent();

        _vm = new ProtractorViewModel();
        BindingContext = _vm;

        ProtractorCanvas.Drawable = new ProtractorDrawable(_vm);

        ProtractorCanvas.StartInteraction += OnStartInteraction;
        ProtractorCanvas.DragInteraction += OnDragInteraction;
        ProtractorCanvas.EndInteraction += OnEndInteraction;
    }

    private void OnStartInteraction(object sender, TouchEventArgs e)
    {
        var touch = e.Touches.FirstOrDefault();
        if (touch == null) return;

        var pos = new Point(touch.X, touch.Y);

        var cx = ProtractorCanvas.Width;
        var cy = ProtractorCanvas.Height / 2;

        var p1 = new Point(cx + _vm.Line1End.X, cy + _vm.Line1End.Y);
        var p2 = new Point(cx + _vm.Line2End.X, cy + _vm.Line2End.Y);

        if (Distance(pos, p1) < Distance(pos, p2))
            _activeLine = 1;
        else
            _activeLine = 2;

        UpdateLine(pos, cx, cy);
    }

    private void OnDragInteraction(object sender, TouchEventArgs e)
    {
        var touch = e.Touches.FirstOrDefault();
        if (touch == null || !_activeLine.HasValue) return;

        var pos = new Point(touch.X, touch.Y);
        var cx = ProtractorCanvas.Width;
        var cy = ProtractorCanvas.Height / 2;

        UpdateLine(pos, cx, cy);
    }

    private void OnEndInteraction(object sender, TouchEventArgs e)
    {
        _activeLine = null;
    }

    private void UpdateLine(Point pos, double cx, double cy)
    {
        var dx = cx - pos.X;
        var dy = pos.Y - cy;

        var vec = new Point(-dx, dy); // flip x to point *into* canvas from right edge

        if (_activeLine == 1)
            _vm.SetLine1(vec);
        else if (_activeLine == 2)
            _vm.SetLine2(vec);

        ProtractorCanvas.Invalidate();
    }

    private static double Distance(Point p1, Point p2) =>
        Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
}
