using Microsoft.Maui.Graphics;
using System;
using System.Globalization;
using System.Linq;
using MAUI_Nonsense_App.Pages.Activity;

namespace MAUI_Nonsense_App.Pages._Drawable
{
    public class WeeklyBarChartDrawable : IDrawable
    {
        private readonly ActivityReportViewModel _vm;
        public WeeklyBarChartDrawable(ActivityReportViewModel vm) => _vm = vm;

        public float GrowthProgress { get; set; } = 1f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            canvas.Antialias = true;

            // ---- Values per metric ----
            double ValueOf(DayStat d) => _vm.SelectedMode switch
            {
                MetricMode.Steps => d.Steps,
                MetricMode.Distance => d.DistanceKm,
                MetricMode.Time => d.Minutes,
                _ => d.Calories
            };
            var vals = _vm.Days.Select(ValueOf).ToArray();

            // ---- Goal (per metric) ----
            double goal = _vm.SelectedMode switch
            {
                MetricMode.Steps => _vm.DailyGoalSteps,
                MetricMode.Distance => _vm.GoalDistanceKm,
                MetricMode.Time => _vm.GoalMinutes,
                MetricMode.Calories => _vm.GoalCalories,
                _ => _vm.DailyGoalSteps
            };

            // ---- Scale (past+today only) ----
            double maxShownVal = 0;
            for (int i = 0; i < _vm.Days.Count; i++)
                if (!_vm.Days[i].IsFuture)
                    maxShownVal = Math.Max(maxShownVal, vals[i]);

            double scaleMax = Math.Max(maxShownVal, _vm.SelectedMode == MetricMode.Steps ? goal : 0);
            if (scaleMax <= 0) scaleMax = 1;
            scaleMax *= 1.15;

            // ---- Ticks ----
            var (tickStep, tickMax, decimals) = GetAxisScale(scaleMax, _vm.SelectedMode);

            // ---- Shared plot layout ----
            string maxLabel = ChartLayout.FormatTick(tickMax, decimals);
            var (plot, axisX, left, right, top, bottom) = ChartLayout.ComputePlot(canvas, dirtyRect, maxLabel);

            // ---- Axes ----
            canvas.StrokeSize = 1;
            canvas.StrokeColor = Colors.LightGray;
            canvas.DrawLine(axisX, top, axisX, plot.Bottom);
            canvas.DrawLine(left, plot.Bottom, right, plot.Bottom);

            // ---- Grid + Y labels ----
            for (double t = 0; t <= tickMax + tickStep * 0.25; t += tickStep)
            {
                float y = (float)(plot.Bottom - (t / tickMax) * plot.Height);

                canvas.StrokeColor = Color.FromArgb("#E5E7EB");
                canvas.DrawLine(left, y, right, y);

                string text = ChartLayout.FormatTick(t, decimals);
                canvas.FontSize = ChartLayout.YLabelFont;
                canvas.FontColor = Colors.Gray;

                float labelRight = axisX - 6f;
                float labelLeft = dirtyRect.Left + ChartLayout.OuterPad;
                float labelW = MathF.Max(0, labelRight - labelLeft);
                canvas.DrawString(text, labelLeft, y - 8, labelW, 16,
                    HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            // ---- Goal line + pill (Steps only) ----
            if (_vm.SelectedMode == MetricMode.Steps)
            {
                float gy = (float)(plot.Bottom - (goal / tickMax) * plot.Height);
                canvas.StrokeColor = Colors.LightGreen.WithAlpha(0.85f);
                canvas.StrokeSize = 2;
                canvas.StrokeDashPattern = new float[] { 6, 6 };
                canvas.DrawLine(left, gy, right, gy);
                canvas.StrokeDashPattern = null;

                DrawGoalTagLeftGutter(canvas, axisX, top, plot.Height, gy, goal, decimals);
            }

            // ---- Bars ----
            int n = 7;
            float gap = 12f;
            float colW = (plot.Width - gap * (n - 1)) / n;
            float x = left;

            var greenToday = Color.FromArgb("#16A34A");
            var greenPast = Color.FromArgb("#22C55E");
            var starGold = Color.FromArgb("#F7C948");
            bool drawStars = GrowthProgress >= 0.999f;

            float[] barTrueTops = new float[n];

            for (int i = 0; i < n; i++)
            {
                var day = _vm.Days[i];
                double v = vals[i];

                float fullBarH = (float)(v / tickMax * plot.Height);
                float barH = fullBarH * GrowthProgress;
                float bx = x;
                float by = plot.Bottom - barH;

                barTrueTops[i] = plot.Bottom - fullBarH;

                if (!day.IsFuture && v > 0)
                {
                    canvas.FillColor = day.IsToday ? greenToday : greenPast;
                    canvas.FillRoundedRectangle(bx, by, colW, barH, 6);

                    if (drawStars && _vm.SelectedMode == MetricMode.Steps && v >= goal && fullBarH > 16)
                    {
                        float cx = bx + colW / 2f;
                        float cy = barTrueTops[i] + 10f;
                        DrawStar(canvas, cx, cy, 7f, 3.5f, starGold);
                    }
                }

                // Numeric labels for past days only (no label for 'today')
                if (!day.IsFuture && !day.IsToday)
                {
                    string label = _vm.SelectedMode switch
                    {
                        MetricMode.Steps => $"{(int)v}",
                        MetricMode.Distance => $"{v:F1}",
                        MetricMode.Time => $"{v:N0}",
                        _ => $"{v:F0}"
                    };

                    canvas.FontSize = 8.0f; // Bar chart labels font size
                    canvas.FontColor = Colors.Gray;
                    float trueTop = barTrueTops[i];
                    float labelY = (v == 0) ? (plot.Bottom - 18) : (trueTop - 10);
                    canvas.DrawString(label, bx, labelY, colW, 10,
                        HorizontalAlignment.Center, VerticalAlignment.Bottom);
                }

                // Weekday labels — unified small font
                const float WeekdayFont = 10f;
                canvas.FontSize = WeekdayFont;
                canvas.FontColor = Colors.Gray;
                float cxMid = bx + colW / 2f;
                canvas.DrawString(day.Abbrev, cxMid, plot.Bottom + 16f, HorizontalAlignment.Center);

                x += colW + gap;
            }

            // ---- Today bubble above the highest column ----
            int ti = Enumerable.Range(0, _vm.Days.Count).FirstOrDefault(i => _vm.Days[i].IsToday, -1);
            if (ti >= 0)
            {
                float tallestTop = barTrueTops
                    .Where((_, idx) => !_vm.Days[idx].IsFuture)
                    .DefaultIfEmpty(plot.Bottom)
                    .Min();

                float todayTop = barTrueTops[ti];
                float colWAll = (plot.Width - gap * (n - 1)) / n;
                float colX = left + ti * (colWAll + gap);
                float cxMid = colX + colWAll / 2;

                var td = _vm.Days[ti];
                double vToday = vals[ti];

                string bubble = _vm.SelectedMode switch
                {
                    MetricMode.Steps => $"{td.Steps:N0} Steps",
                    MetricMode.Distance => $"{td.DistanceKm:F2} km",
                    MetricMode.Time => $"{td.Minutes:N0} min",
                    _ => $"{td.Calories:F0} kcal"
                };

                float bw = Math.Max(colWAll + 40, 120);
                float bh = 34f;
                float pointerH = 8f;
                float minGap = 10f;
                float extraCeiling = 6f;

                float refTop = Math.Min(todayTop, tallestTop);
                float ryTarget = refTop - (bh + pointerH + minGap);
                float ry = MathF.Max(top + extraCeiling, ryTarget);

                float rx = cxMid - bw / 2;
                canvas.FillColor = Color.FromArgb("#34D399");
                canvas.FillRoundedRectangle(rx, ry, bw, bh, 8);

                canvas.FontSize = 14;
                canvas.FontColor = Colors.White;
                canvas.DrawString(bubble, rx, ry, bw, bh,
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

        // ----- helpers -----

        private (double step, double maxTick, int decimals) GetAxisScale(double scaleMax, MetricMode mode)
        {
            int targetTicks = 4;
            double rawStep = scaleMax / targetTicks;
            double step = NiceStep(rawStep);

            if (mode == MetricMode.Distance) step = Math.Max(0.1, step);
            else step = Math.Max(1, step);

            int decimals = (mode == MetricMode.Distance && step < 1) ? 1 : 0;
            double maxTick = Math.Ceiling(scaleMax / step) * step;
            return (step, maxTick, decimals);
        }

        private static double NiceStep(double x)
        {
            if (x <= 0) return 1;
            double exp = Math.Floor(Math.Log10(x));
            double f = x / Math.Pow(10, exp);
            double nice = (f < 1.5) ? 1 : (f < 3) ? 2 : (f < 7) ? 5 : 10;
            return nice * Math.Pow(10, exp);
        }

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
            float rx = axisX - bw - 6f;

            const float linkW = 4f;
            const float linkH = 8f;
            float lx = axisX - linkW;
            float ly = gy - linkH / 2f;

            var fill = Color.FromArgb("#86EFAC");
            var text = Color.FromArgb("#065F46");

            canvas.FillColor = fill;
            canvas.FillRoundedRectangle(lx, ly, linkW, linkH, linkH / 2f);
            canvas.FillRoundedRectangle(rx, ry, bw, bh, 6f);

            canvas.FontSize = 11;
            canvas.FontColor = text;
            canvas.DrawString(txt, rx, ry, bw, bh,
                HorizontalAlignment.Center, VerticalAlignment.Center);
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
}
