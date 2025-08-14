using Microsoft.Maui.Graphics;
using System.Globalization;

namespace MAUI_Nonsense_App.Pages._Drawable;

public static class ChartLayout
{
    // Global look
    public const float OuterPad = 8f;   // card edge breathing room
    public const float TopPad = 12f;
    public const float BottomPad = 12f;
    public const float AxisXGap = 8f;   // gap between y-labels and plot
    public const float AxisBand = 30f;  // space under plot for X labels (same in all charts)
    public const float YLabelFont = 11f;  // consistent Y label font

    /// <summary>
    /// Compute plot rectangle and Y-axis X coordinate with a measured gutter
    /// wide enough for the *largest* Y-axis label.
    /// </summary>
    public static (RectF plot, float axisX, float left, float right, float top, float bottom)
        ComputePlot(ICanvas canvas, RectF rect, string maxYLabel)
    {
        // Measure max Y label width once (fixed font for consistency)
        canvas.FontSize = YLabelFont;
        var size = canvas.GetStringSize(
            maxYLabel.Replace(' ', '\u00A0'), // keep thousands together
            Microsoft.Maui.Graphics.Font.Default,
            YLabelFont);

        float gutter = MathF.Max(56f, size.Width + 12f); // min gutter + breathing
        float left = rect.Left + OuterPad + gutter + AxisXGap;
        float right = rect.Right - OuterPad;
        float top = rect.Top + TopPad;
        float bottom = rect.Bottom - BottomPad;

        var plot = new RectF(left, top, MathF.Max(0, right - left), MathF.Max(0, (bottom - top) - AxisBand));
        float axisX = left - AxisXGap;
        return (plot, axisX, left, right, top, bottom);
    }

    public static string FormatTick(double value, int decimals)
        => decimals == 0
            ? value.ToString("N0", CultureInfo.CurrentCulture).Replace(' ', '\u00A0')
            : value.ToString($"F{decimals}", CultureInfo.CurrentCulture);
}
