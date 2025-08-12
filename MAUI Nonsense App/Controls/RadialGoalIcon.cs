// Controls/RadialGoalIcon.cs
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace MAUI_Nonsense_App.Controls;

public class RadialGoalIcon : GraphicsView, IDrawable
{
    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(RadialGoalIcon), 0.0,
            propertyChanged: (b, o, n) => ((RadialGoalIcon)b).Invalidate());

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(RadialGoalIcon), 24.0,
            propertyChanged: (b, o, n) => ((RadialGoalIcon)b).Invalidate());

    public static readonly BindableProperty AchievedProperty =
        BindableProperty.Create(nameof(Achieved), typeof(bool), typeof(RadialGoalIcon), false,
            propertyChanged: (b, o, n) => ((RadialGoalIcon)b).Invalidate());

    // Optional color customization
    public static readonly BindableProperty TrackColorProperty =
        BindableProperty.Create(nameof(TrackColor), typeof(Color), typeof(RadialGoalIcon), new Color(0.85f, 0.87f, 0.90f));

    public static readonly BindableProperty ProgressColorProperty =
        BindableProperty.Create(nameof(ProgressColor), typeof(Color), typeof(RadialGoalIcon), new Color(0.62f, 0.64f, 0.69f));

    public static readonly BindableProperty AchievedFillColorProperty =
        BindableProperty.Create(nameof(AchievedFillColor), typeof(Color), typeof(RadialGoalIcon), new Color(0.38f, 0.40f, 0.45f));

    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public bool Achieved { get => (bool)GetValue(AchievedProperty); set => SetValue(AchievedProperty, value); }

    public Color TrackColor { get => (Color)GetValue(TrackColorProperty); set => SetValue(TrackColorProperty, value); }
    public Color ProgressColor { get => (Color)GetValue(ProgressColorProperty); set => SetValue(ProgressColorProperty, value); }
    public Color AchievedFillColor { get => (Color)GetValue(AchievedFillColorProperty); set => SetValue(AchievedFillColorProperty, value); }

    public RadialGoalIcon()
    {
        Drawable = this;
        HeightRequest = WidthRequest = Size;
        BackgroundColor = Colors.Transparent;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var s = (float)Size;
        var cx = s / 2f;
        var cy = s / 2f;
        var r = s * 0.45f;

        canvas.SaveState();
        canvas.Antialias = true;
        canvas.StrokeSize = s * 0.10f;

        // Track ring
        canvas.StrokeColor = TrackColor;
        canvas.DrawCircle(cx, cy, r);

        // Progress arc (use absolute END angle, not sweep!)
        var p = Math.Clamp(Progress, 0, 1);
        if (p > 0)
        {
            var start = -90f;
            var end = (float)(start + 360.0 * p); // <-- FIX
            canvas.StrokeColor = ProgressColor;
            canvas.DrawArc(cx - r, cy - r, r * 2, r * 2, start, end, false, false);
        }

        // Full fill when achieved
        if (Achieved)
        {
            canvas.FillColor = AchievedFillColor;
            canvas.FillCircle(cx, cy, r * 0.65f);
        }

        canvas.RestoreState();
    }
}
