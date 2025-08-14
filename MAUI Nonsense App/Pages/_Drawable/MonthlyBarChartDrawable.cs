using Microsoft.Maui.Graphics;
using System.Linq;

namespace MAUI_Nonsense_App.Pages._Drawable
{
    public class MonthlyBarChartDrawable : IDrawable
    {
        private readonly Activity.ActivityReportViewModel _vm;
        public MonthlyBarChartDrawable(Activity.ActivityReportViewModel vm) => _vm = vm;

        public float GrowthProgress { get; set; } = 1f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            canvas.Antialias = true;

            // ---- Layout with left gutter for Y-axis ----
            const float AxisLabelWidth = 56f;
            const float OuterPad = 8f;
            float left = dirtyRect.Left + OuterPad + AxisLabelWidth + 6f;
            float right = dirtyRect.Right - 12f;
            float top = dirtyRect.Top + 12f;
            float bottom = dirtyRect.Bottom - 26f; // room for day labels
            float width = System.Math.Max(0, right - left);
            float height = System.Math.Max(0, bottom - top);
            float axisX = left - 8f;

            var axis = Color.FromArgb("#D1D5DB");
            var grid = Color.FromArgb("#E5E7EB");
            var label = Color.FromArgb("#6B7280");
            var bar = Color.FromArgb("#22C55E");
            var barToday = Color.FromArgb("#16A34A");
            var goalCol = Color.FromArgb("#86EFAC");

            var days = _vm.MonthDays?.ToList() ?? new();
            if (days.Count == 0) { canvas.RestoreState(); return; }

            // scale: past + today only
            double maxShown = 1;
            for (int i = 0; i < days.Count; i++)
                if (!days[i].IsFuture)
                    maxShown = System.Math.Max(maxShown, days[i].Steps);

            double scaleMax = System.Math.Max(maxShown, _vm.DailyGoalSteps);
            if (scaleMax <= 0) scaleMax = 1;
            scaleMax *= 1.15;

            var (tickStep, tickMax, decimals) = GetAxisScale(scaleMax);

            // axes
            canvas.StrokeSize = 1;
            canvas.StrokeColor = axis;
            canvas.DrawLine(left, bottom, right, bottom); // X
            canvas.DrawLine(axisX, top, axisX, bottom);   // Y

            // grid + Y labels (small font)
            DrawYAxis(canvas, axisX, left, right, top, height, tickStep, tickMax, decimals);

            // goal dashed line
            if (_vm.DailyGoalSteps > 0)
            {
                float gy = (float)(top + height - (_vm.DailyGoalSteps / tickMax) * height);
                canvas.StrokeColor = goalCol;
                canvas.StrokeSize = 2;
                canvas.StrokeDashPattern = new float[] { 6, 4 };
                canvas.DrawLine(left, gy, right, gy);
                canvas.StrokeDashPattern = null;
            }

            // clamp once
            float grow = Clamp01(GrowthProgress);

            // bars
            int n = days.Count;
            float colW = width / n;
            float barW = System.MathF.Max(1.5f, colW * 0.6f);

            int todayIndex = -1;
            float todayTrueTop = 0;
            float todayColCenter = 0;

            for (int i = 0; i < n; i++)
            {
                var d = days[i];
                float fullH = (float)(height * (d.Steps / (double)tickMax));
                float h = fullH * grow;
                if (h > 0)
                {
                    float bx = left + i * colW + (colW - barW) / 2f;
                    float by = bottom - h;
                    canvas.FillColor = d.IsToday ? barToday : bar;
                    canvas.FillRoundedRectangle(bx, by, barW, h, 2);

                    if (d.IsToday)
                    {
                        todayIndex = i;
                        todayTrueTop = bottom - fullH;
                        todayColCenter = bx + barW / 2f;
                    }
                }
            }

            // day labels (every 5th)
            canvas.FontColor = label;
            canvas.FontSize = 10;
            for (int i = 0; i < n; i += 5)
            {
                string t = days[i].Date.Day.ToString();
                float xCenter = left + i * colW + colW / 2f;
                canvas.DrawString(t, xCenter - 12, bottom + 2, 24, 14,
                    HorizontalAlignment.Center, VerticalAlignment.Top);
            }

            // Today-only green bubble
            if (todayIndex >= 0)
            {
                var d = days[todayIndex];
                string bubble = $"{d.Steps:N0} Steps";

                float bw = System.MathF.Max(colW + 40, 120);
                float bh = 34f;
                float pointerH = 8f;
                float minGap = 10f;
                float extraCeiling = 6f;

                float refTop = todayTrueTop;
                float ryTarget = refTop - (bh + pointerH + minGap);
                float ry = System.MathF.Max(top + extraCeiling, ryTarget);
                float rx = todayColCenter - bw / 2f;

                canvas.FillColor = Color.FromArgb("#34D399");
                canvas.FillRoundedRectangle(rx, ry, bw, bh, 8f);

                canvas.FontSize = 14;
                canvas.FontColor = Colors.White;
                canvas.DrawString(bubble, rx, ry, bw, bh,
                    HorizontalAlignment.Center, VerticalAlignment.Center);

                var pointer = new PathF();
                pointer.MoveTo(todayColCenter - 6, ry + bh);
                pointer.LineTo(todayColCenter + 6, ry + bh);
                pointer.LineTo(todayColCenter, ry + bh + pointerH);
                pointer.Close();
                canvas.FillPath(pointer);
            }

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
    }
}
