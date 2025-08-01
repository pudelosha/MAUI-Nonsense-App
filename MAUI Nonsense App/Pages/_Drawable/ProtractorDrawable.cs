using MAUI_Nonsense_App.ViewModels;
using Microsoft.Maui.Graphics;

public class ProtractorDrawable : IDrawable
{
    private readonly ProtractorViewModel _vm;

    public ProtractorDrawable(ProtractorViewModel vm)
    {
        _vm = vm;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float cx = dirtyRect.Right;
        float cy = dirtyRect.Height / 2;

        float radius = Math.Min(dirtyRect.Width, dirtyRect.Height / 2) - 20;

        // Background
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);

        // Draw pale gray circle background
        canvas.FillColor = Colors.LightGray.WithAlpha(0.3f);
        canvas.FillEllipse(cx - radius, cy - radius, radius * 2, radius * 2);

        // Circle border
        canvas.StrokeColor = Colors.Gray;
        canvas.StrokeSize = 2;
        canvas.DrawEllipse(cx - radius, cy - radius, radius * 2, radius * 2);

        // Tick marks & custom labels
        for (int angle = 0; angle < 360; angle++)
        {
            double rad = angle * Math.PI / 180.0;

            float cos = (float)Math.Cos(rad);
            float sin = (float)Math.Sin(rad);

            float xOuter = cx + radius * cos;
            float yOuter = cy + radius * sin;

            float xInner;
            float yInner;

            bool isLabeledAngle =
                (angle == 90 || angle == 120 || angle == 150 || angle == 180 ||
                 angle == 210 || angle == 240 || angle == 270);

            if (isLabeledAngle)
            {
                xInner = cx + (radius - 10) * cos;
                yInner = cy + (radius - 10) * sin;
            }
            else
            {
                // Small tick mark
                xInner = cx + (radius - 5) * cos;
                yInner = cy + (radius - 5) * sin;
            }

            canvas.StrokeSize = 1;
            canvas.DrawLine(xOuter, yOuter, xInner, yInner);

            // Draw labels at specific angles
            if (isLabeledAngle)
            {
                float xText = cx + (radius - 25) * cos;
                float yText = cy + (radius - 25) * sin;

                int labelValue = 0;

                if (angle == 90) labelValue = 0;
                else if (angle == 120) labelValue = 30;
                else if (angle == 150) labelValue = 60;
                else if (angle == 180) labelValue = 90;
                else if (angle == 210) labelValue = 60;
                else if (angle == 240) labelValue = 30;
                else if (angle == 270) labelValue = 0;

                float labelOffsetX = (angle == 90 || angle == 270) ? -10 : 0;

                canvas.FontSize = 12;
                canvas.FontColor = Colors.Black;
                canvas.DrawString($"{labelValue}", xText - 8 + labelOffsetX, yText - 8, 16, 16,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }

        // Draw center point
        canvas.FillColor = Colors.Black;
        canvas.FillCircle(cx, cy, 5);

        // Draw red lines
        canvas.StrokeColor = Colors.Red;
        DrawLineAtAngle(canvas, cx, cy, radius, _vm.Angle1);
        DrawLineAtAngle(canvas, cx, cy, radius, _vm.Angle2);

        // Draw angle text
        canvas.FontColor = Colors.Black;
        canvas.FontSize = 24;
        canvas.DrawString($"{_vm.AngleBetween:F1}°", cx - radius / 2, cy - 20,
            HorizontalAlignment.Center);
    }

    private void DrawLineAtAngle(ICanvas canvas, float cx, float cy, float radius, double angle)
    {
        double rad = angle * Math.PI / 180.0;
        float x = cx + (float)(radius * Math.Cos(rad));
        float y = cy + (float)(radius * Math.Sin(rad));
        canvas.DrawLine(cx, cy, x, y);
    }
}
