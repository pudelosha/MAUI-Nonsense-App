using Microsoft.Maui.Graphics;
using System;
using System.Globalization;
using System.Linq;
using MAUI_Nonsense_App.Pages.Activity;

namespace MAUI_Nonsense_App.Pages._Drawable
{
    public class MonthlyBarChartDrawable : IDrawable
    {
        private readonly ActivityReportViewModel _vm;
        public MonthlyBarChartDrawable(ActivityReportViewModel vm) => _vm = vm;

        public float GrowthProgress { get; set; } = 1f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            canvas.Antialias = true;

            var days = _vm.MonthDays?.ToList() ?? new();
            if (days.Count == 0) { canvas.RestoreState(); return; }

            // ---- Values per metric ----
            double ValueOf(DayStat d) => _vm.SelectedMode switch
            {
                MetricMode.Steps => d.Steps,
                MetricMode.Distance => d.DistanceKm,
                MetricMode.Time => d.Minutes,
                _ => d.Calories
            };
            var vals = days.Select(ValueOf).ToArray();

            // ---- Scale (include daily goal in Steps, and ensure >= one tick above it) ----
            double maxShown = vals.Max();
            if (_vm.SelectedMode == MetricMode.Steps)
                maxShown = Math.Max(maxShown, _vm.DailyGoalSteps);
            if (maxShown <= 0) maxShown = 1;
            maxShown *= 1.15;

            var (tickStep, tickMax, decimals) = GetAxisScale(maxShown, _vm.SelectedMode);

            if (_vm.SelectedMode == MetricMode.Steps && _vm.DailyGoalSteps > 0 && tickMax <= _vm.DailyGoalSteps)
            {
                tickMax = Math.Ceiling((_vm.DailyGoalSteps + tickStep) / tickStep) * tickStep;
            }

            // ---- Shared plot layout ----
            string maxLabel = ChartLayout.FormatTick(tickMax, decimals);
            var (plot, axisX, left, right, top, bottom) = ChartLayout.ComputePlot(canvas, dirtyRect, maxLabel);

            // ---- Axes ----
            canvas.StrokeSize = 1;
            canvas.StrokeColor = Colors.LightGray;
            canvas.DrawLine(axisX, top, axisX, plot.Bottom);
            canvas.DrawLine(left, plot.Bottom, right, plot.Bottom);

            // ---- Grid + Y labels (no units) ----
            for (double t = 0; t <= tickMax + tickStep * 0.25; t += tickStep)
            {
                float y = (float)(plot.Bottom - (t / tickMax) * plot.Height);
                canvas.StrokeColor = Color.FromArgb("#E5E7EB");
                canvas.DrawLine(left, y, right, y);

                string text = ChartLayout.FormatTick(t, decimals);
                canvas.FontSize = ChartLayout.YLabelFont;
                canvas.FontColor = Color.FromArgb("#6B7280");
                float labelRight = axisX - 6f;
                float labelLeft = dirtyRect.Left + ChartLayout.OuterPad;
                float labelW = MathF.Max(0, labelRight - labelLeft);
                canvas.DrawString(text, labelLeft, y - 8, labelW, 16,
                    HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            // ---- Dashed daily goal (Steps only) + left gutter pill ----
            if (_vm.SelectedMode == MetricMode.Steps && _vm.DailyGoalSteps > 0)
            {
                float gy = (float)(plot.Bottom - (_vm.DailyGoalSteps / tickMax) * plot.Height);
                canvas.StrokeColor = Color.FromArgb("#86EFAC");
                canvas.StrokeSize = 2;
                canvas.StrokeDashPattern = new float[] { 6, 6 };
                canvas.DrawLine(left, gy, right, gy);
                canvas.StrokeDashPattern = null;

                DrawGoalTagLeftGutter(canvas, axisX, top, plot.Height, gy,
                    _vm.DailyGoalSteps, decimals,
                    pillFill: Color.FromArgb("#86EFAC"),
                    textCol: Color.FromArgb("#065F46"));
            }

            // ---- Columns ----
            float colW = plot.Width / days.Count;
            float barW = Math.Max(1.5f, colW * 0.6f);
            float grow = Math.Clamp(GrowthProgress, 0f, 1f);

            int todayIndex = -1;
            float todayTop = 0, todayCenterX = 0;

            var barPast = Color.FromArgb("#22C55E");
            var barToday = Color.FromArgb("#16A34A");

            for (int i = 0; i < days.Count; i++)
            {
                double v = vals[i];
                float fullH = (float)(plot.Height * (v / tickMax));
                float h = fullH * grow;
                if (h <= 0) continue;

                float bx = plot.Left + i * colW + (colW - barW) / 2f;
                float by = plot.Bottom - h;

                canvas.FillColor = days[i].IsToday ? barToday : barPast;
                canvas.FillRoundedRectangle(bx, by, barW, h, 2);

                if (days[i].IsToday)
                {
                    todayIndex = i;
                    todayTop = plot.Bottom - fullH; // true (ungrown) top
                    todayCenterX = bx + barW / 2f;
                }
            }

            // ---- Day labels (every 5th, unified small font) ----
            canvas.FontSize = 10f;
            canvas.FontColor = Color.FromArgb("#6B7280");
            for (int i = 0; i < days.Count; i += 5)
            {
                string dTxt = days[i].Date.Day.ToString();
                float cx = plot.Left + i * colW + colW / 2f;
                canvas.DrawString(dTxt, cx, plot.Bottom + 14f, HorizontalAlignment.Center);
            }

            // ---- Green bubble above TODAY only ----
            if (todayIndex >= 0 && GrowthProgress >= 0.98f)
            {
                var v = vals[todayIndex];
                string text = _vm.SelectedMode switch
                {
                    MetricMode.Steps => $"{v:N0} Steps",
                    MetricMode.Distance => $"{v:F2} km",
                    MetricMode.Time => $"{v:N0} min",
                    _ => $"{v:F0} kcal"
                };

                float bw = Math.Max(120f, 9f * text.Length);
                float bh = 28f;
                float pointerH = 8f;
                float gap = 8f;

                float ry = todayTop - (bh + pointerH + gap);
                ry = MathF.Max(top + 6, ry);
                float rx = todayCenterX - bw / 2f;

                canvas.FillColor = Color.FromArgb("#34D399");
                canvas.FillRoundedRectangle(rx, ry, bw, bh, 8);

                canvas.FontColor = Colors.White;
                canvas.FontSize = 13;
                canvas.DrawString(text, rx, ry, bw, bh,
                    HorizontalAlignment.Center, VerticalAlignment.Center);

                var p = new PathF();
                p.MoveTo(todayCenterX - 6, ry + bh);
                p.LineTo(todayCenterX + 6, ry + bh);
                p.LineTo(todayCenterX, ry + bh + pointerH);
                p.Close();
                canvas.FillPath(p);
            }

            canvas.RestoreState();
        }

        // ----- helpers -----

        // Minimum tick steps: Distance >= 0.1 ; others >= 1
        private static (double step, double maxTick, int decimals) GetAxisScale(double scaleMax, MetricMode mode)
        {
            int targetTicks = 4;
            double raw = scaleMax / targetTicks;
            double step = NiceStep(raw);

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

        // Goal tag in the left gutter (aligned with dashed line)
        private static void DrawGoalTagLeftGutter(
            ICanvas canvas, float axisX, float top, float height, float gy,
            double goal, int decimals, Color pillFill, Color textCol)
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

            canvas.FillColor = pillFill;
            canvas.FillRoundedRectangle(lx, ly, linkW, linkH, linkH / 2f);
            canvas.FillRoundedRectangle(rx, ry, bw, bh, 6f);

            canvas.FontSize = 11;
            canvas.FontColor = textCol;
            canvas.DrawString(txt, rx, ry, bw, bh,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
