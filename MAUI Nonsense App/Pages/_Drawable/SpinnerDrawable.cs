using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages._Drawable
{
    public class SpinnerDrawable : IDrawable
    {
        private readonly RandomSpinnerViewModel _viewModel;
        private readonly Color[] _colors = new Color[]
        {
            Colors.Red, Colors.Green, Colors.Blue, Colors.Orange, Colors.Purple,
            Colors.Cyan, Colors.Brown, Colors.Magenta, Colors.Teal, Colors.Yellow
        };

        public SpinnerDrawable(RandomSpinnerViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var center = new PointF(dirtyRect.Width / 2, dirtyRect.Height / 2);
            float radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2 - 10;
            float anglePerSlice = 360f / _viewModel.Options.Count;

            canvas.SaveState();
            canvas.Translate(center.X, center.Y);
            canvas.Rotate(_viewModel.CurrentAngle);

            for (int i = 0; i < _viewModel.Options.Count; i++)
            {
                float startAngle = i * anglePerSlice - 90f; // Offset to start from top
                float startAngleRad = (float)(Math.PI * startAngle / 180);

                var path = new PathF();
                path.MoveTo(0, 0);

                // Arc drawing with line approximation
                int steps = 20;
                float angleStep = anglePerSlice / steps;
                for (int s = 0; s <= steps; s++)
                {
                    float angleDeg = startAngle + s * angleStep;
                    float angleRad = (float)(Math.PI * angleDeg / 180);
                    float x = radius * (float)Math.Cos(angleRad);
                    float y = radius * (float)Math.Sin(angleRad);
                    path.LineTo(x, y);
                }

                path.Close();

                // Fill slice
                canvas.FillColor = _colors[i % _colors.Length];
                canvas.FillPath(path);

                // Draw label pointing outward (upright relative to slice)
                canvas.SaveState();

                float midAngle = startAngle + anglePerSlice / 2;
                float midAngleRad = (float)(Math.PI * midAngle / 180);

                float textX = (radius * 0.65f) * (float)Math.Cos(midAngleRad);
                float textY = (radius * 0.65f) * (float)Math.Sin(midAngleRad);

                canvas.Translate(textX, textY);
                canvas.Rotate(midAngle); // rotate with slice
                canvas.FontColor = Colors.White;
                canvas.FontSize = 14;

                string label = _viewModel.Options[i];
                canvas.DrawString(
                    label,
                    -50, -10,
                    100, 20,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center
                );

                canvas.RestoreState();
            }

            canvas.RestoreState();

            // Draw triangle pointer at top center
            float triangleSize = 15;
            var pointer = new PathF();
            pointer.MoveTo(center.X, 0);
            pointer.LineTo(center.X - triangleSize, -triangleSize);
            pointer.LineTo(center.X + triangleSize, -triangleSize);
            pointer.Close();

            canvas.FillColor = Colors.Black;
            canvas.FillPath(pointer);
        }
    }
}
