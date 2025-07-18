using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages._Drawable
{
    public class ProtractorDrawable : IDrawable
    {
        private readonly ProtractorViewModel _vm;

        public ProtractorDrawable(ProtractorViewModel vm)
        {
            _vm = vm;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);

            float cx = dirtyRect.Center.X;
            float cy = dirtyRect.Bottom;
            float radius = Math.Min(dirtyRect.Width / 2, dirtyRect.Height);

            // Protractor arc
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawArc(cx - radius, cy - radius, radius * 2, radius * 2, 180, 180, false, false);

            // Tick marks & numbers
            for (int i = 0; i <= 180; i += 10)
            {
                double rad = Math.PI * i / 180.0;
                float x1 = cx + (float)(radius * Math.Cos(rad));
                float y1 = cy - (float)(radius * Math.Sin(rad));
                float x2 = cx + (float)((radius - 15) * Math.Cos(rad));
                float y2 = cy - (float)((radius - 15) * Math.Sin(rad));

                canvas.DrawLine(x1, y1, x2, y2);

                if (i % 30 == 0)
                {
                    float xText = cx + (float)((radius - 30) * Math.Cos(rad));
                    float yText = cy - (float)((radius - 30) * Math.Sin(rad));
                    canvas.DrawString($"{i}", xText - 10, yText - 10, 20, 20,
                        HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }

            // Lines
            canvas.StrokeColor = Colors.Red;
            canvas.StrokeSize = 2;

            var p1 = _vm.Line1End;
            var p2 = _vm.Line2End;

            canvas.DrawLine(cx, cy, cx + (float)p1.X, cy - (float)p1.Y);
            canvas.DrawLine(cx, cy, cx + (float)p2.X, cy - (float)p2.Y);

            // Center circle
            canvas.FillColor = Colors.Gray;
            canvas.FillCircle(cx, cy, 5);

            // Angle text
            canvas.FontSize = 48;
            canvas.FontColor = Colors.Black;
            canvas.DrawString($"{_vm.Angle:0.00}°", cx - 60, cy - radius / 2, 120, 60,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
