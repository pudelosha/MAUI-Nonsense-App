using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages.Survival;

public class CompassDrawable : IDrawable
{
    public double Heading { get; set; } = 0;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float verticalAdjustment = 0f;

        float cx = dirtyRect.Center.X;
        float cy = dirtyRect.Center.Y - verticalAdjustment;

        float radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2 - 10;

        // Outer circle fill with light pale gray (similar to tile background)
        canvas.FillColor = Color.FromArgb("#F0F0F0");  // adjust if needed
        canvas.FillCircle(cx, cy, radius + 10);

        // Outer circle stroke is transparent
        canvas.StrokeColor = Colors.Transparent;
        canvas.StrokeSize = 2;
        canvas.DrawCircle(cx, cy, radius);

        canvas.SaveState();

        // Rotate canvas by heading
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

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawLine(xInner, yInner, xOuter, yOuter);

            // Labels
            canvas.SaveState();

            float textRadius = radius - 25;
            float xText = (float)(textRadius * Math.Sin(rad));
            float yText = (float)(-textRadius * Math.Cos(rad));

            canvas.Translate(xText, yText);
            canvas.Rotate((float)Heading);

            string label = angle.ToString();
            bool isCardinal = false;

            if (angle == 0) { label = "N"; isCardinal = true; }
            else if (angle == 90) { label = "E"; isCardinal = true; }
            else if (angle == 180) { label = "S"; isCardinal = true; }
            else if (angle == 270) { label = "W"; isCardinal = true; }

            var font = isCardinal ? Microsoft.Maui.Graphics.Font.DefaultBold : Microsoft.Maui.Graphics.Font.Default;
            var fontSize = isCardinal ? 20f : 12f;

            SizeF size = canvas.GetStringSize(label, font, fontSize);
            float textYOffset = size.Height / 2;

            canvas.Font = font;
            canvas.FontSize = fontSize;
            canvas.FontColor = Colors.Black;

            canvas.DrawString(label, 0, textYOffset, HorizontalAlignment.Center);

            canvas.RestoreState();
        }

        // Small ticks every 10°
        for (int angle = 10; angle < 360; angle += 10)
        {
            float tickLength = 5;
            float innerRadius = radius - tickLength;

            double rad = Math.PI * angle / 180.0;
            float xOuter = (float)(radius * Math.Sin(rad));
            float yOuter = (float)(-radius * Math.Cos(rad));

            float xInner = (float)(innerRadius * Math.Sin(rad));
            float yInner = (float)(-innerRadius * Math.Cos(rad));

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1;
            canvas.DrawLine(xInner, yInner, xOuter, yOuter);
        }

        // Very tiny ticks every 2°
        for (int angle = 2; angle < 360; angle += 2)
        {
            // Skip if this is a 10° tick (to avoid overlapping)
            if (angle % 10 == 0) continue;

            float tickLength = 2;
            float innerRadius = radius - tickLength;

            double rad = Math.PI * angle / 180.0;
            float xOuter = (float)(radius * Math.Sin(rad));
            float yOuter = (float)(-radius * Math.Cos(rad));

            float xInner = (float)(innerRadius * Math.Sin(rad));
            float yInner = (float)(-innerRadius * Math.Cos(rad));

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1;
            canvas.DrawLine(xInner, yInner, xOuter, yOuter);
        }

        canvas.RestoreState();

        // Draw needle: red sharp triangle pointing up
        canvas.FillColor = Colors.Red;
        var needle = new PathF();
        needle.MoveTo(cx, cy - radius);  // tip
        needle.LineTo(cx - 5, cy);
        needle.LineTo(cx + 5, cy);
        needle.Close();
        canvas.FillPath(needle);

        // Draw opposite needle: light gray triangle pointing down
        canvas.FillColor = Colors.LightGray;
        var backNeedle = new PathF();
        backNeedle.MoveTo(cx, cy + radius);  // tip down
        backNeedle.LineTo(cx - 5, cy);
        backNeedle.LineTo(cx + 5, cy);
        backNeedle.Close();
        canvas.FillPath(backNeedle);
    }
}
