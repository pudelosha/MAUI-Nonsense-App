using Microsoft.Maui.Graphics;
using System;
using System.Globalization;
using MAUI_Nonsense_App.Pages.Activity;

namespace MAUI_Nonsense_App.Pages._Drawable
{
    public class DailyHourlyChartDrawable : IDrawable
    {
        private readonly ActivityReportViewModel _vm;
        public DailyHourlyChartDrawable(ActivityReportViewModel vm) => _vm = vm;

        public float GrowthProgress { get; set; } = 1f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            canvas.Antialias = true;

            // ---- Data -> selected metric ----
            var steps = _vm.HourlyStepsForDay ?? new int[24];
            double[] vals = new double[24];
            double maxVal = 0;
            for (int i = 0; i < 24; i++)
            {
                vals[i] = _vm.ConvertSteps(steps[i], _vm.SelectedMode);
                if (vals[i] > maxVal) maxVal = vals[i];
            }
            if (maxVal <= 0) maxVal = 1;

            // ---- Scale (nice ticks + minimum steps) ----
            var (tickStep, tickMax, decimals) = GetAxisScale(maxVal, _vm.SelectedMode);

            // ---- Shared plot layout ----
            string maxLabel = ChartLayout.FormatTick(tickMax, decimals);
            var (plot, axisX, left, right, top, bottom) = ChartLayout.ComputePlot(canvas, dirtyRect, maxLabel);

            // ---- Axes ----
            canvas.StrokeSize = 1;
            canvas.StrokeColor = Colors.LightGray;
            canvas.DrawLine(axisX, top, axisX, plot.Bottom);      // Y
            canvas.DrawLine(left, plot.Bottom, right, plot.Bottom); // X

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

            // ---- Bars ----
            int n = 24;
            float colW = plot.Width / n;
            float barW = Math.Max(2, colW * 0.52f);
            float grow = Math.Clamp(GrowthProgress, 0f, 1f);

            canvas.FillColor = Color.FromArgb("#22C55E"); // emerald
            for (int i = 0; i < n; i++)
            {
                double v = vals[i];
                if (v <= 0) continue;

                float hFull = (float)(plot.Height * (v / tickMax));
                float h = hFull * grow;

                float x = plot.Left + i * colW + (colW - barW) / 2f;
                float y = plot.Bottom - h;
                canvas.FillRoundedRectangle(x, y, barW, h, 3);
            }

            // ---- X ticks (06 / 12 / 18) unified small font ----
            canvas.FontSize = 10f;
            canvas.FontColor = Color.FromArgb("#6B7280");
            DrawHourTick(canvas, plot, 6, "06");
            DrawHourTick(canvas, plot, 12, "12");
            DrawHourTick(canvas, plot, 18, "18");

            canvas.RestoreState();
        }

        private static void DrawHourTick(ICanvas c, RectF plot, int hour, string text)
        {
            float n = 24f;
            float colW = plot.Width / n;
            float cx = plot.Left + hour * colW + colW / 2f;
            c.DrawString(text, cx, plot.Bottom + 14f, HorizontalAlignment.Center);
        }

        // Min tick: Distance >= 0.1 ; others >= 1
        private static (double step, double maxTick, int decimals) GetAxisScale(double scaleMax, MetricMode mode)
        {
            int targetTicks = 4;
            double raw = scaleMax / targetTicks;
            double step = NiceStep(raw);
            if (mode == MetricMode.Distance)
                step = Math.Max(0.1, step);
            else
                step = Math.Max(1, step);

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
    }
}
