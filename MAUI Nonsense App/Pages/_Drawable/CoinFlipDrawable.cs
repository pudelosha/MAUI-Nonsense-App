using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages._Drawable
{
    public class CoinFlipDrawable : IDrawable
    {
        public double CurrentAngle { get; set; }
        public string Face { get; set; } = "Heads"; // or "Tails"

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float centerX = dirtyRect.Width / 2;
            float centerY = dirtyRect.Height / 2;
            float radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2 - 10;

            canvas.SaveState();

            // Simulate 3D rotation
            double radians = CurrentAngle * Math.PI / 180;
            double scaleX = Math.Cos(radians);

            canvas.Translate(centerX, centerY);
            canvas.Scale((float)scaleX, 1);

            // Colors
            Color frameColor = Colors.Goldenrod.WithLuminosity(0.4f);
            Color frameHighlight = frameColor.WithLuminosity(0.6f);

            // Outer frame
            canvas.FillColor = frameColor;
            canvas.FillCircle(0, 0, radius);

            // Inner circle: gradient fill
            var gradient = new LinearGradientPaint
            {
                StartColor = Colors.Gold,
                EndColor = Colors.Goldenrod,
                StartPoint = new Point(-radius, -radius),
                EndPoint = new Point(radius, radius)
            };

            canvas.SetFillPaint(
                gradient,
                new RectF(
                    -radius * 0.92f,
                    -radius * 0.92f,
                    radius * 1.84f,
                    radius * 1.84f
                )
            );
            canvas.FillCircle(0, 0, radius * 0.92f);

            // Frame edges & highlights
            canvas.StrokeSize = 4;
            canvas.StrokeColor = frameHighlight;
            canvas.DrawCircle(0, 0, radius);
            canvas.DrawCircle(0, 0, radius * 0.92f);

            canvas.StrokeColor = frameColor;
            canvas.DrawCircle(0, 0, radius * 0.96f);
            canvas.DrawCircle(0, 0, radius * 0.88f);

            // Use emoji
            string symbol = Face switch
            {
                "Heads" => "👤",   // heads — bust
                "Tails" => "🦅",   // tails — eagle
                _ => Face
            };

            canvas.Font = Microsoft.Maui.Graphics.Font.Default;
            canvas.FontSize = radius * 0.5f;
            canvas.FontColor = frameColor; // has no effect on emoji

            canvas.DrawString(
                symbol,
                -radius / 2,
                -radius / 2,
                radius,
                radius,
                HorizontalAlignment.Center,
                VerticalAlignment.Center
            );

            canvas.RestoreState();
        }
    }
}
