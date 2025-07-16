using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages.Survival;

public class CompassDrawable : IDrawable
{
    public double Heading { get; set; } = 0;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Adjust center vertically for better balance
        const float verticalAdjustment = 0f;

        float cx = dirtyRect.Center.X;
        float cy = dirtyRect.Center.Y - verticalAdjustment;

        float radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2 - 10;

        // Background
        canvas.FillColor = Colors.White;
        canvas.FillCircle(cx, cy, radius + 10);

        // Outer circle
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 2;
        canvas.DrawCircle(cx, cy, radius);

        canvas.SaveState();

        // Rotate the canvas by the heading
        canvas.Translate(cx, cy);
        canvas.Rotate((float)-Heading);

        // Draw ticks and labels
        for (int angle = 0; angle < 360; angle += 30)
        {
            float tickLength = 10;
            float innerRadius = radius - tickLength;

            double rad = Math.PI * angle / 180.0;
            float xOuter = (float)(radius * Math.Sin(rad));
            float yOuter = (float)(-radius * Math.Cos(rad));

            float xInner = (float)(innerRadius * Math.Sin(rad));
            float yInner = (float)(-innerRadius * Math.Cos(rad));

            canvas.DrawLine(xInner, yInner, xOuter, yOuter);

            // Draw text upright (compensate rotation)
            canvas.SaveState();

            float textRadius = radius - 25;  // inside circle
            float xText = (float)(textRadius * Math.Sin(rad));
            float yText = (float)(-textRadius * Math.Cos(rad));

            canvas.Translate(xText, yText);
            canvas.Rotate((float)Heading); // keep upright

            string label = angle.ToString();
            bool isCardinal = false;

            switch (angle)
            {
                case 0: label = "N"; isCardinal = true; break;
                case 90: label = "E"; isCardinal = true; break;
                case 180: label = "S"; isCardinal = true; break;
                case 270: label = "W"; isCardinal = true; break;
            }

            var font = isCardinal ? Microsoft.Maui.Graphics.Font.DefaultBold : Microsoft.Maui.Graphics.Font.Default;
            var fontSize = isCardinal ? 20f : 12f;

            // Measure string size to vertically center
            SizeF size = canvas.GetStringSize(label, font, fontSize);
            float textYOffset = size.Height / 2;

            canvas.Font = font;
            canvas.FontSize = fontSize;
            canvas.FontColor = Colors.Black;

            canvas.DrawString(label, 0, textYOffset, HorizontalAlignment.Center);

            canvas.RestoreState();
        }

        canvas.RestoreState();

        // Fixed red needle
        canvas.StrokeColor = Colors.Red;
        canvas.StrokeSize = 4;
        canvas.DrawLine(cx, cy, cx, cy - radius);
    }
}
