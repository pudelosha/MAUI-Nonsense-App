using Microsoft.Maui.Graphics;
using System.Linq;
using MAUI_Nonsense_App.Pages.Activity;

namespace MAUI_Nonsense_App.Pages._Drawable;

public class WeeklyBarChartDrawable : IDrawable
{
    private readonly ActivityReportViewModel _vm;
    public WeeklyBarChartDrawable(ActivityReportViewModel vm) => _vm = vm;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Antialias = true;

        // layout
        float left = 24, right = dirtyRect.Width - 12, top = 12, bottom = dirtyRect.Height - 12;
        float axisLabelBand = 30;                   // reserved for weekday names
        float width = right - left;
        float height = (bottom - top) - axisLabelBand;

        // values per selected metric
        double ValueOf(DayStat d) => _vm.SelectedMode switch
        {
            MetricMode.Steps => d.Steps,
            MetricMode.Distance => d.DistanceKm,
            MetricMode.Time => d.Minutes,
            _ => d.Calories
        };
        var vals = _vm.Days.Select(ValueOf).ToArray();

        // goal per mode
        double goal = _vm.SelectedMode switch
        {
            MetricMode.Steps => _vm.DailyGoalSteps,
            MetricMode.Distance => _vm.GoalDistanceKm,
            MetricMode.Time => _vm.GoalMinutes,
            MetricMode.Calories => _vm.GoalCalories,
            _ => _vm.DailyGoalSteps
        };

        // Max among past+today (ignore future) and the goal
        double maxShownVal = 0;
        for (int i = 0; i < _vm.Days.Count; i++)
            if (!_vm.Days[i].IsFuture)
                maxShownVal = Math.Max(maxShownVal, vals[i]);

        double maxVal = Math.Max(maxShownVal, goal);
        if (maxVal <= 0) maxVal = 1;
        maxVal *= 1.15; // headroom

        // columns layout
        int n = 7;
        float gap = 12f;
        float colW = (width - gap * (n - 1)) / n;
        float x = left;

        // goal line
        float gy = (float)(top + height - (goal / maxVal * height));
        canvas.StrokeColor = Colors.LightGreen.WithAlpha(0.85f);
        canvas.StrokeSize = 2;
        canvas.StrokeDashPattern = new float[] { 6, 6 };
        canvas.DrawLine(left, gy, right, gy);
        canvas.StrokeDashPattern = null;

        // colors
        var greenToday = Color.FromArgb("#16A34A"); // darker
        var greenPast = Color.FromArgb("#22C55E"); // lighter
        var starGold = Color.FromArgb("#F7C948"); // gold

        // draw bars
        for (int i = 0; i < n; i++)
        {
            var day = _vm.Days[i];
            double v = vals[i];
            float barH = (float)(v / maxVal * height);
            float bx = x;
            float by = top + height - barH;

            if (!day.IsFuture && v > 0)
            {
                canvas.FillColor = day.IsToday ? greenToday : greenPast;
                canvas.FillRoundedRectangle(bx, by, colW, barH, 6);

                // star if goal reached (inside bar at top)
                if (v >= goal && barH > 16)
                {
                    float cx = bx + colW / 2f;
                    float cy = by + 10f;          // slightly below the top edge
                    DrawStar(canvas, cx, cy, 7f, 3.5f, starGold);
                }
            }

            // numeric label for past days (show zero too), not for today, not for future
            if (!day.IsFuture && !day.IsToday)
            {
                string label = _vm.SelectedMode switch
                {
                    MetricMode.Steps => $"{day.Steps}",
                    MetricMode.Distance => $"{day.DistanceKm:F1} km",
                    MetricMode.Time => $"{day.Minutes:N0} m",
                    _ => $"{day.Calories:F0}"
                };

                canvas.FontSize = 10;
                canvas.FontColor = Colors.Gray;

                float labelY = (v == 0) ? top + height - 18 : by - 10;
                canvas.DrawString(label, bx, labelY, colW, 10,
                    HorizontalAlignment.Center, VerticalAlignment.Bottom);
            }

            // weekday name (all 7)
            canvas.FontSize = 12;
            canvas.FontColor = Colors.Gray;
            canvas.DrawString(day.Abbrev, bx, top + height + 2, colW, 20,
                HorizontalAlignment.Center, VerticalAlignment.Top);

            x += colW + gap;
        }

        // bubble for today (kept as before, placed above tallest bar)
        int ti = Enumerable.Range(0, _vm.Days.Count).FirstOrDefault(i => _vm.Days[i].IsToday, -1);
        if (ti >= 0)
        {
            float bx = left + ti * (colW + gap);
            float bxCenter = bx + colW / 2;

            var td = _vm.Days[ti];
            string bubble = _vm.SelectedMode switch
            {
                MetricMode.Steps => $"{td.Steps:N0} Steps",
                MetricMode.Distance => $"{td.DistanceKm:F2} km",
                MetricMode.Time => $"{td.Minutes:N0} min",
                _ => $"{td.Calories:F0} kcal"
            };

            const float bubbleH = 34f;
            const float margin = 14f;
            float highestTopY = (float)(top + height - (float)(maxShownVal / maxVal * height));
            float ry = highestTopY - bubbleH - margin;
            if (ry < top + 6) ry = top + 6;

            float bubbleW = Math.Max(colW + 40, 120);
            float rx = bxCenter - bubbleW / 2;

            canvas.FillColor = Color.FromArgb("#34D399");
            canvas.FillRoundedRectangle(rx, ry, bubbleW, bubbleH, 8);

            canvas.FontSize = 14;
            canvas.FontColor = Colors.White;
            canvas.DrawString(bubble, rx, ry, bubbleW, bubbleH,
                HorizontalAlignment.Center, VerticalAlignment.Center);

            var pointer = new PathF();
            pointer.MoveTo(bxCenter - 6, ry + bubbleH);
            pointer.LineTo(bxCenter + 6, ry + bubbleH);
            pointer.LineTo(bxCenter, ry + bubbleH + 8);
            pointer.Close();
            canvas.FillPath(pointer);
        }

        canvas.RestoreState();
    }

    private static void DrawStar(ICanvas canvas, float cx, float cy, float rOuter, float rInner, Color color)
    {
        var path = new PathF();
        const int points = 5;
        double angle = -Math.PI / 2;          // start at top
        double step = Math.PI / points;

        path.MoveTo(cx + (float)(Math.Cos(angle) * rOuter), cy + (float)(Math.Sin(angle) * rOuter));
        for (int i = 0; i < points; i++)
        {
            angle += step;
            path.LineTo(cx + (float)(Math.Cos(angle) * rInner), cy + (float)(Math.Sin(angle) * rInner));
            angle += step;
            path.LineTo(cx + (float)(Math.Cos(angle) * rOuter), cy + (float)(Math.Sin(angle) * rOuter));
        }
        path.Close();

        canvas.FillColor = color;
        canvas.FillPath(path);
    }
}
