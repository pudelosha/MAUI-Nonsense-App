using Microsoft.Maui.Graphics;
using System;
using System.Globalization;
using System.Linq;
using MAUI_Nonsense_App.Pages.Activity;

namespace MAUI_Nonsense_App.Pages._Drawable;

public class WeeklyBarChartDrawable : IDrawable
{
    private readonly ActivityReportViewModel _vm;
    public WeeklyBarChartDrawable(ActivityReportViewModel vm) => _vm = vm;

    /// <summary>0..1 easing factor for "grow from bottom" animation.</summary>
    public float GrowthProgress { get; set; } = 1f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Antialias = true;

        // -------- Layout --------
        const float AxisLabelWidth = 64f; // wide enough for "10 000"
        const float OuterPad = 8f;
        float left = OuterPad + AxisLabelWidth + 6f;
        float right = dirtyRect.Width - 12f;
        float top = 12f;
        float bottom = dirtyRect.Height - 12f;
        float axisBand = 34f;                 // more room for weekday labels
        float width = right - left;
        float height = (bottom - top) - axisBand;
        float axisX = left - 8f;             // Y-axis line

        static bool IsFuture(DayStat d) => d.Date > DateTime.UtcNow.Date;

        // -------- Values per metric --------
        double ValueOf(DayStat d) => _vm.SelectedMode switch
        {
            MetricMode.Steps => d.Steps,
            MetricMode.Distance => d.DistanceKm,
            MetricMode.Time => d.Minutes,
            _ => d.Calories
        };
        var vals = _vm.Days.Select(ValueOf).ToArray();

        // -------- Goal (per metric) --------
        double goal = _vm.SelectedMode switch
        {
            MetricMode.Steps => _vm.DailyGoalSteps,
            MetricMode.Distance => _vm.GoalDistanceKm,
            MetricMode.Time => _vm.GoalMinutes,
            MetricMode.Calories => _vm.GoalCalories,
            _ => _vm.DailyGoalSteps
        };

        // -------- Scale --------
        double maxShownVal = 0;
        for (int i = 0; i < _vm.Days.Count; i++)
            if (!IsFuture(_vm.Days[i]))
                maxShownVal = Math.Max(maxShownVal, vals[i]);

        double scaleMax = Math.Max(maxShownVal, goal);
        if (scaleMax <= 0) scaleMax = 1;
        scaleMax *= 1.15; // headroom

        // -------- Y-axis ticks (nice numbers) --------
        var (tickStep, tickMax, decimals) = GetAxisScale(scaleMax, _vm.SelectedMode);
        DrawYAxis(canvas, axisX, left, right, top, height, tickStep, tickMax, decimals);

        // -------- Goal line + tag at Y-axis (only for STEPS) --------
        float gy = 0f;
        if (_vm.SelectedMode == MetricMode.Steps)
        {
            gy = (float)(top + height - (goal / scaleMax * height));
            canvas.StrokeColor = Colors.LightGreen.WithAlpha(0.85f);
            canvas.StrokeSize = 2;
            canvas.StrokeDashPattern = new float[] { 6, 6 };
            canvas.DrawLine(left, gy, right, gy);
            canvas.StrokeDashPattern = null;

            DrawGoalTagLeftGutter(canvas, axisX, top, height, gy, goal, decimals);
        }

        // -------- Bars --------
        int n = 7;
        float gap = 12f;
        float colW = (width - gap * (n - 1)) / n;
        float x = left;

        var greenToday = Color.FromArgb("#16A34A"); // darker for today
        var greenPast = Color.FromArgb("#22C55E"); // lighter for past days
        var starGold = Color.FromArgb("#F7C948"); // gold star
        bool drawStars = GrowthProgress >= 0.999f;  // ⭐ only when grow finished

        // Track bar heights and where other-day labels sit,
        // so we can place today's bubble safely above everything.
        float[] fullBarHeights = new float[n];
        float minOtherLabelTopY = float.MaxValue; // smaller Y = higher up

        for (int i = 0; i < n; i++)
        {
            var day = _vm.Days[i];
            double v = vals[i];

            float fullBarH = (float)(v / scaleMax * height);
            fullBarHeights[i] = fullBarH;

            float barH = fullBarH * GrowthProgress;
            float bx = x;
            float by = top + height - barH;

            if (!IsFuture(day) && v > 0)
            {
                canvas.FillColor = day.IsToday ? greenToday : greenPast;
                canvas.FillRoundedRectangle(bx, by, colW, barH, 6);

                // star only in Steps mode
                if (drawStars && _vm.SelectedMode == MetricMode.Steps && v >= goal && fullBarH > 16)
                {
                    float cx = bx + colW / 2f;
                    float cy = top + height - fullBarH + 10f; // lock to true top
                    DrawStar(canvas, cx, cy, 7f, 3.5f, starGold);
                }
            }

            // numeric labels for past days only (not today, not future)
            if (!IsFuture(day) && !day.IsToday)
            {
                string label = _vm.SelectedMode switch
                {
                    MetricMode.Steps => $"{day.Steps}",
                    MetricMode.Distance => $"{day.DistanceKm:F1} km",
                    MetricMode.Time => $"{day.Minutes:N0} m",
                    _ => $"{day.Calories:F0}"
                };

                float labelFont = 9f; // smaller to avoid "collapse"
                var sz = canvas.GetStringSize(label, Microsoft.Maui.Graphics.Font.Default, labelFont);
                if (sz.Width > colW - 2)
                {
                    // shrink proportionally (keep ≥ 7.5px)
                    float scale = MathF.Max(0.75f, (colW - 2) / sz.Width);
                    labelFont *= scale;
                }

                canvas.FontSize = labelFont;
                canvas.FontColor = Colors.Gray;

                float trueTop = top + height - fullBarH;
                float labelBottomY = (v == 0) ? top + height - 18 : trueTop - 10;
                float labelTopY = labelBottomY - labelFont - 1;

                // Track highest label (smallest Y)
                if (v > 0)
                    minOtherLabelTopY = MathF.Min(minOtherLabelTopY, labelTopY);

                canvas.DrawString(label, bx, labelBottomY, colW, labelFont + 2,
                    HorizontalAlignment.Center, VerticalAlignment.Bottom);
            }

            // --- Weekday names: single line, auto shrink to fit, never wrap
            string dayText = day.Abbrev;
            float dayFont = 11f;
            var daySize = canvas.GetStringSize(dayText, Microsoft.Maui.Graphics.Font.Default, dayFont);
            if (daySize.Width > colW - 2)
            {
                float scale = MathF.Max(0.75f, (colW - 2) / daySize.Width);
                dayFont *= scale;
            }
            canvas.FontSize = dayFont;
            canvas.FontColor = Colors.Gray;

            float cxMid = bx + colW / 2f;
            canvas.DrawString(dayText, cxMid, top + height + 16f, HorizontalAlignment.Center);

            x += colW + gap;
        }

        // -------- Today bubble: above TALLEST weekly bar and above any past-day labels --------
        int ti = Enumerable.Range(0, _vm.Days.Count).FirstOrDefault(i => _vm.Days[i].IsToday, -1);
        if (ti >= 0)
        {
            float tallestTop = (float)(top + height - (float)(maxShownVal / scaleMax * height));
            float todayTop = top + height - fullBarHeights[ti];

            float colX = left + ti * (colW + gap);
            float cxMid = colX + colW / 2;

            var td = _vm.Days[ti];
            string bubbleText = _vm.SelectedMode switch
            {
                MetricMode.Steps => $"{td.Steps:N0} Steps",
                MetricMode.Distance => $"{td.DistanceKm:F2} km",
                MetricMode.Time => $"{td.Minutes:N0} min",
                _ => $"{td.Calories:F0} kcal"
            };

            const float bh = 34f; // bubble height
            const float pointerH = 8f;
            const float baseMargin = 30f; // headroom above tallest bar
            const float minGapFromBars = 12f; // ensure bubble pointer is above today's bar

            // Base on tallest bar to avoid covering other labels by default.
            float ry = tallestTop - bh - baseMargin;

            // Make sure the pointer tip is ABOVE today's bar by min gap.
            float maxPointerTipY = todayTop - minGapFromBars; // smaller y = higher on screen
            if (ry + bh + pointerH > maxPointerTipY)
                ry = maxPointerTipY - (bh + pointerH);

            // Also ensure the pointer tip is ABOVE the highest other-day label.
            if (minOtherLabelTopY != float.MaxValue)
            {
                float maxTipY = minOtherLabelTopY - 2f; // just above label top
                if (ry + bh + pointerH > maxTipY)
                    ry = maxTipY - (bh + pointerH);
            }

            if (ry < top + 6) ry = top + 6;

            float bw = Math.Max(colW + 40, 120);
            float rx = cxMid - bw / 2;

            canvas.FillColor = Color.FromArgb("#34D399");
            canvas.FillRoundedRectangle(rx, ry, bw, bh, 8);

            canvas.FontSize = 14;
            canvas.FontColor = Colors.White;
            canvas.DrawString(bubbleText, rx, ry, bw, bh,
                HorizontalAlignment.Center, VerticalAlignment.Center);

            var pointer = new PathF();
            pointer.MoveTo(cxMid - 6, ry + bh);
            pointer.LineTo(cxMid + 6, ry + bh);
            pointer.LineTo(cxMid, ry + bh + pointerH);
            pointer.Close();
            canvas.FillPath(pointer);
        }

        canvas.RestoreState();
    }

    // ---------- helpers ----------

    private (double step, double maxTick, int decimals) GetAxisScale(double scaleMax, MetricMode mode)
    {
        // target ~5 ticks (0 + 4 intervals)
        int targetTicks = 4;
        double rawStep = scaleMax / targetTicks;
        double step = NiceStep(rawStep); // 1/2/5 * 10^n

        int decimals = 0;
        if (mode == MetricMode.Distance && step < 1) decimals = 1;

        double maxTick = Math.Ceiling(scaleMax / step) * step;
        return (step, maxTick, decimals);
    }

    private static double NiceStep(double x)
    {
        if (x <= 0) return 1;
        double exp = Math.Floor(Math.Log10(x));
        double f = x / Math.Pow(10, exp); // 1..10
        double nice = (f < 1.5) ? 1 : (f < 3) ? 2 : (f < 7) ? 5 : 10;
        return nice * Math.Pow(10, exp);
    }

    private void DrawYAxis(
        ICanvas canvas, float axisX, float left, float right, float top, float height,
        double tickStep, double tickMax, int decimals)
    {
        // axis line
        canvas.StrokeColor = Colors.LightGray;
        canvas.StrokeSize = 1;
        canvas.DrawLine(axisX, top, axisX, top + height);

        // ticks + grid
        for (double t = 0; t <= tickMax + tickStep * 0.25; t += tickStep)
        {
            float y = (float)(top + height - (t / tickMax) * height);

            // grid line
            canvas.StrokeColor = Color.FromArgb("#E5E7EB");
            canvas.DrawLine(left, y, right, y);

            // label (use NBSP to avoid breaking "10 000")
            string text = decimals == 0
                ? t.ToString("N0", CultureInfo.CurrentCulture)
                : t.ToString($"F{decimals}", CultureInfo.CurrentCulture);
            text = text.Replace(' ', '\u00A0'); // NBSP

            canvas.FontSize = 11;
            canvas.FontColor = Colors.Gray;

            float labelRight = axisX - 6f;
            float labelLeft = 8f;
            float labelW = labelRight - labelLeft; // ~AxisLabelWidth
            canvas.DrawString(text, labelLeft, y - 8, labelW, 16,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }
    }

    // Goal tag drawn in the left gutter (at the Y-axis), never covered by bars
    private void DrawGoalTagLeftGutter(
        ICanvas canvas, float axisX, float top, float height, float gy, double goal, int decimals)
    {
        string txt = decimals == 0
            ? goal.ToString("N0", CultureInfo.CurrentCulture)
            : goal.ToString($"F{decimals}", CultureInfo.CurrentCulture);
        txt = txt.Replace(' ', '\u00A0');

        const float bh = 20f;
        float ry = gy - bh / 2f;
        ry = MathF.Max(top + 2, MathF.Min(top + height - bh - 2, ry));

        float bw = MathF.Max(48f, 8f * txt.Length + 16f);
        float rx = axisX - bw - 6f; // to the left of the axis

        // small connector to axis
        const float linkW = 4f;
        const float linkH = 8f;
        float lx = axisX - linkW;
        float ly = gy - linkH / 2f;

        var fill = Color.FromArgb("#86EFAC");
        var text = Color.FromArgb("#065F46");

        canvas.FillColor = fill;
        canvas.FillRoundedRectangle(lx, ly, linkW, linkH, linkH / 2f); // vertical link
        canvas.FillRoundedRectangle(rx, ry, bw, bh, 6f);               // pill

        canvas.FontSize = 11;
        canvas.FontColor = text;
        canvas.DrawString(txt, rx, ry, bw, bh, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private static void DrawStar(ICanvas canvas, float cx, float cy, float rOuter, float rInner, Color color)
    {
        var path = new PathF();
        const int points = 5;
        double angle = -Math.PI / 2;
        double step = Math.PI / points;

        path.MoveTo(cx + (float)(Math.Cos(angle) * rOuter),
                    cy + (float)(Math.Sin(angle) * rOuter));
        for (int i = 0; i < points; i++)
        {
            angle += step;
            path.LineTo(cx + (float)(Math.Cos(angle) * rInner),
                        cy + (float)(Math.Sin(angle) * rInner));
            angle += step;
            path.LineTo(cx + (float)(Math.Cos(angle) * rOuter),
                        cy + (float)(Math.Sin(angle) * rOuter));
        }
        path.Close();

        canvas.FillColor = color;
        canvas.FillPath(path);
    }
}
