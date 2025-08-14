using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages._Drawable
{
    public class DailyHourlyChartDrawable : IDrawable
    {
        private readonly Activity.ActivityReportViewModel _vm;
        public DailyHourlyChartDrawable(Activity.ActivityReportViewModel vm) => _vm = vm;

        // 0..1 grow-in animation
        public float GrowthProgress { get; set; } = 1f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            canvas.Antialias = true;

            // ---- Layout with left gutter for Y-axis labels ----
            const float AxisLabelWidth = 56f;   // small numbers fit
            const float OuterPad = 8f;
            float left = dirtyRect.Left + OuterPad + AxisLabelWidth + 6f;
            float right = dirtyRect.Right - 12f;
            float top = dirtyRect.Top + 12f;
            float bottom = dirtyRect.Bottom - 26f; // room for hour labels
            float width = System.Math.Max(0, right - left);
            float height = System.Math.Max(0, bottom - top);
            float axisX = left - 8f;

            var axis = Color.FromArgb("#D1D5DB");
            var grid = Color.FromArgb("#E5E7EB");
            var label = Color.FromArgb("#6B7280");
            var bar = Color.FromArgb("#22C55E"); // emerald

            // data / scale
            var hours = _vm.HourlyStepsForDay ?? new int[24];
            int maxVal = System.Math.Max(1, hours.Max());
            double scaleMax = maxVal * 1.15; // headroom

            // "nice" tick step for small Y labels
            var (tickStep, tickMax, decimals) = GetAxisScale(scaleMax);

            // axes
            canvas.StrokeSize = 1;
            canvas.StrokeColor = axis;
            canvas.DrawLine(left, bottom, right, bottom); // X
            canvas.DrawLine(axisX, top, axisX, bottom);   // Y

            // grid + Y labels
            DrawYAxis(canvas, axisX, left, right, top, height, tickStep, tickMax, decimals);

            // clamp once
            float grow = Clamp01(GrowthProgress);

            // 24 bars
            int n = 24;
            float colW = width / n;
            float barW = System.Math.Max(2f, colW * 0.52f);

            canvas.FillColor = bar;
            for (int i = 0; i < n; i++)
            {
                float v = hours[i];
                if (v <= 0) continue;

                float h = (float)(height * (v / tickMax) * grow);
                if (h <= 0) continue;

                float x = left + i * colW + (colW - barW) / 2f;
                float y = bottom - h;
                canvas.FillRoundedRectangle(x, y, barW, h, 3);
            }

            // hour ticks (06 / 12 / 18)
            canvas.FontColor = label;
            canvas.FontSize = 10;
            DrawHourTick(canvas, left, bottom, width, 24, 6, "06");
            DrawHourTick(canvas, left, bottom, width, 24, 12, "12");
            DrawHourTick(canvas, left, bottom, width, 24, 18, "18");

            canvas.RestoreState();
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        private static (double step, double maxTick, int decimals) GetAxisScale(double scaleMax)
        {
            if (scaleMax <= 0) return (1, 1, 0);
            int targetTicks = 4;
            double rawStep = scaleMax / targetTicks;
            double step = NiceStep(rawStep);
            double maxTick = System.Math.Ceiling(scaleMax / step) * step;
            return (step, maxTick, 0);
        }

        private static double NiceStep(double x)
        {
            if (x <= 0) return 1;
            double exp = System.Math.Floor(System.Math.Log10(x));
            double f = x / System.Math.Pow(10, exp); // 1..10
            double nice = (f < 1.5) ? 1 : (f < 3) ? 2 : (f < 7) ? 5 : 10;
            return nice * System.Math.Pow(10, exp);
        }

        private static void DrawYAxis(
            ICanvas canvas, float axisX, float left, float right, float top, float height,
            double tickStep, double tickMax, int decimals)
        {
            // grid + labels (small font)
            for (double t = 0; t <= tickMax + tickStep * 0.25; t += tickStep)
            {
                float y = (float)(top + height - (t / tickMax) * height);

                canvas.StrokeColor = Color.FromArgb("#E5E7EB");
                canvas.StrokeSize = 1;
                canvas.DrawLine(left, y, right, y);

                string text = decimals == 0 ? t.ToString("N0") : t.ToString($"F{decimals}");
                text = text.Replace(' ', '\u00A0');

                canvas.FontSize = 10;
                canvas.FontColor = Colors.Gray;

                float labelRight = axisX - 6f;
                float labelLeft = 8f;
                float labelW = labelRight - labelLeft;
                canvas.DrawString(text, labelLeft, y - 7, labelW, 14,
                    HorizontalAlignment.Right, VerticalAlignment.Center);
            }
        }

        private static void DrawHourTick(ICanvas c, float left, float bottom, float width, float n, int hour, string text)
        {
            float colW = width / n;
            float xCenter = left + hour * colW + colW / 2f;
            c.DrawString(text, xCenter - 16, bottom + 2, 32, 14,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }
}
