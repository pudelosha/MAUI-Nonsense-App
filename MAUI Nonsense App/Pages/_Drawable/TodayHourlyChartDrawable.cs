using Microsoft.Maui.Graphics;
using System.Linq;

namespace MAUI_Nonsense_App.Pages._Drawable
{
    /// <summary>Simple 24-bar chart for today's hourly steps.</summary>
    public class TodayHourlyChartDrawable : IDrawable
    {
        /// <summary>24-length array; each item is steps in that hour [0..23].</summary>
        public int[] Hours { get; set; } = new int[24];

        /// <summary>0..1 easing factor if you want a tiny grow animation.</summary>
        public float GrowthProgress { get; set; } = 1f;

        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.SaveState();
            canvas.Antialias = true;

            const float outerPad = 12f;
            const float bottomBand = 26f;          // space for 06:00 / 12:00 / 18:00

            float left = outerPad;
            float right = rect.Width - outerPad;
            float top = outerPad;
            float bottom = rect.Height - outerPad;
            float height = (bottom - top) - bottomBand;
            float width = (right - left);
            if (width <= 0 || height <= 0) { canvas.RestoreState(); return; }

            // Bars layout
            int n = 24;
            float gap = System.MathF.Min(4f, width / (n * 3f));
            float colW = (width - gap * (n - 1)) / n;
            if (colW < 1f) colW = 1f;

            // Scale
            int max = System.Math.Max(1, Hours?.Max() ?? 1);

            // Soft neutral colors (matches your pale cards)
            var gridColor = Color.FromArgb("#E5E7EB");
            var barColor = Color.FromArgb("#B0B7C3"); // muted gray
            var tickColor = Colors.Gray;

            // Light horizontal grid (quarters)
            canvas.StrokeColor = gridColor;
            canvas.StrokeSize = 1;
            for (int g = 1; g <= 3; g++)
            {
                float gy = top + height * (1 - g / 4f);
                canvas.DrawLine(left, gy, right, gy);
            }

            // Bars
            float x = left;
            for (int h = 0; h < n; h++)
            {
                int v = (Hours != null && h < Hours.Length) ? Hours[h] : 0;
                float fullH = (float)v / max * height;
                float barH = fullH * GrowthProgress;
                float by = top + height - barH;

                if (v > 0)
                {
                    canvas.FillColor = barColor;
                    canvas.FillRoundedRectangle(x, by, colW, barH, 3);
                }

                x += colW + gap;
            }

            // Bottom ticks at 06:00 / 12:00 / 18:00
            canvas.FontSize = 11;
            canvas.FontColor = tickColor;
            void DrawTick(int hour, string label)
            {
                float cx = left + hour * (colW + gap) + colW / 2f;
                canvas.DrawString(label, cx, top + height + 14f, HorizontalAlignment.Center);
            }
            DrawTick(6, "06:00");
            DrawTick(12, "12:00");
            DrawTick(18, "18:00");

            canvas.RestoreState();
        }
    }
}
