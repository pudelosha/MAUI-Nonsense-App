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
            propertyChanged: (b, o, n) =>
            {
                var c = (RadialGoalIcon)b;
                var s = (double)n;
                c.WidthRequest = c.HeightRequest = s; // reaguj na zmianę Size
                c.Invalidate();
            });

    public static readonly BindableProperty AchievedProperty =
        BindableProperty.Create(nameof(Achieved), typeof(bool), typeof(RadialGoalIcon), false,
            propertyChanged: (b, o, n) => ((RadialGoalIcon)b).Invalidate());

    // Kolory (z invalidacją po zmianie)
    public static readonly BindableProperty TrackColorProperty =
        BindableProperty.Create(nameof(TrackColor), typeof(Color), typeof(RadialGoalIcon),
            Color.FromArgb("#E5E7EB"), // jasny szary tor
            propertyChanged: (b, o, n) => ((RadialGoalIcon)b).Invalidate());

    public static readonly BindableProperty ProgressColorProperty =
        BindableProperty.Create(nameof(ProgressColor), typeof(Color), typeof(RadialGoalIcon),
            Color.FromArgb("#22C55E"), // ZIELONY jak w Today bar
            propertyChanged: (b, o, n) => ((RadialGoalIcon)b).Invalidate());

    public static readonly BindableProperty AchievedFillColorProperty =
        BindableProperty.Create(nameof(AchievedFillColor), typeof(Color), typeof(RadialGoalIcon),
            Color.FromArgb("#22C55E"), // ZIELONA "kropka" po osiągnięciu
            propertyChanged: (b, o, n) => ((RadialGoalIcon)b).Invalidate());

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
        var stroke = MathF.Max(1f, s * 0.10f);
        var inset = stroke / 2f + 1f; // keep a 1px breathing room
        var rect = new RectF(inset, inset, s - 2 * inset, s - 2 * inset);
        var cx = s / 2f;
        var cy = s / 2f;
        var r = rect.Width / 2f;

        canvas.SaveState();
        canvas.Antialias = true;
        canvas.StrokeSize = stroke;

        // Track ring
        canvas.StrokeColor = TrackColor;
        canvas.DrawCircle(cx, cy, r);

        // Progress arc (absolute END angle)
        var p = Math.Clamp(Progress, 0, 1);
        if (p > 0)
        {
            var start = -90f;
            var end = (float)(start + 360.0 * p);
            canvas.StrokeColor = ProgressColor;
            canvas.DrawArc(rect.X, rect.Y, rect.Width, rect.Height, start, end, false, false);
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
