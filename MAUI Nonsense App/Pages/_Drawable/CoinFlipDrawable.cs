using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages._Drawable
{
    // Keep this exact name
    public class CoinFlipDrawable : IDrawable
    {
        public double CurrentAngle { get; set; }     // degrees
        public string Face { get; set; } = "Heads";  // "Heads" or "Tails"

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float cx = dirtyRect.Width * 0.5f;
            float cy = dirtyRect.Height * 0.5f;
            float r = MathF.Min(dirtyRect.Width, dirtyRect.Height) * 0.5f - 8f;

            // Colors for a silver/steel look
            var rimDark = Color.FromArgb("#9AA0A6");
            var rimLight = Color.FromArgb("#CBD0D6");
            var faceLight = Color.FromArgb("#E9ECEF");
            var faceDark = Color.FromArgb("#B8BDC4");

            // ----- draw coin with horizontal squash (pseudo 3D flip) -----
            canvas.SaveState();
            float scaleX = (float)Math.Cos(CurrentAngle * Math.PI / 180.0);
            canvas.Translate(cx, cy);
            canvas.Scale(scaleX, 1f);

            // Outer rim
            canvas.FillColor = rimDark;
            canvas.FillCircle(0, 0, r);

            // Inner face (silver gradient)
            var grad = new LinearGradientPaint
            {
                StartColor = faceLight,
                EndColor = faceDark,
                StartPoint = new PointF(-r, -r),
                EndPoint = new PointF(r, r)
            };
            var faceRect = new RectF(-r * 0.9f, -r * 0.9f, r * 1.8f, r * 1.8f);
            canvas.SetFillPaint(grad, faceRect);
            canvas.FillCircle(0, 0, r * 0.9f);

            // Rim lines
            canvas.StrokeSize = 3f;
            canvas.StrokeColor = rimLight;
            canvas.DrawCircle(0, 0, r);
            canvas.DrawCircle(0, 0, r * 0.92f);

            canvas.StrokeColor = rimDark.WithAlpha(0.35f);
            canvas.DrawCircle(0, 0, r * 0.86f);

            canvas.RestoreState(); // back to normal (no squash)

            // ----- face symbol (emoji), not squashed -----
            canvas.SaveState();
            canvas.Translate(cx, cy);

            string emoji = Face == "Heads" ? "👤" : "🦅";
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;
            canvas.FontSize = r * 0.55f;
            var textRect = new RectF(-r * 0.5f, -r * 0.58f, r, r);
            canvas.DrawString(emoji, textRect,
                HorizontalAlignment.Center, VerticalAlignment.Center);

            canvas.RestoreState();
        }
    }
}
