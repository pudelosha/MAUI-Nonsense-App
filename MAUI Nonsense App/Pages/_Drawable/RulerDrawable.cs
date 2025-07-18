using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages.Tools
{
    public class RulerDrawable : IDrawable
    {
        private readonly RulerViewModel _vm;
        private readonly bool _isRight;
        private readonly Color _backgroundGray = Color.FromArgb("#f1f1f1");

        public RulerDrawable(RulerViewModel viewModel, bool isRight = false)
        {
            _vm = viewModel;
            _isRight = isRight;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = _backgroundGray;
            canvas.FillRectangle(dirtyRect);

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1;

            double dipsPerUnit = _vm.DipsPerUnit;
            double height = dirtyRect.Height;

            int totalUnits = (int)Math.Ceiling(height / dipsPerUnit);

            for (int i = 0; i <= totalUnits; i++)
            {
                float y = (float)(i * dipsPerUnit);

                if (_isRight)
                {
                    float rightEdge = dirtyRect.Width;

                    // Draw long tick on right edge
                    canvas.DrawLine(rightEdge, y, rightEdge - 20, y);

                    // Half tick
                    if (i < totalUnits)
                    {
                        float halfY = y + (float)(dipsPerUnit / 2);
                        canvas.DrawLine(rightEdge, halfY, rightEdge - 10, halfY);
                    }

                    // Draw number on **left edge of right ruler**
                    canvas.FontSize = 12;
                    canvas.DrawString($"{i}", 5, y - 6, HorizontalAlignment.Left);
                }
                else
                {
                    // Left ruler
                    canvas.DrawLine(0, y, 20, y);

                    if (i < totalUnits)
                    {
                        float halfY = y + (float)(dipsPerUnit / 2);
                        canvas.DrawLine(0, halfY, 10, halfY);
                    }

                    canvas.FontSize = 12;
                    canvas.DrawString($"{i}", dirtyRect.Width - 5, y - 6, HorizontalAlignment.Right);
                }
            }

            // Unit label
            canvas.FontSize = 14;

            if (_isRight)
            {
                // Right ruler: label on left
                canvas.DrawString(_vm.UnitLabel, 5, 15, HorizontalAlignment.Left);
            }
            else
            {
                // Left ruler: label on right
                canvas.DrawString(_vm.UnitLabel, dirtyRect.Width - 5, 15, HorizontalAlignment.Right);
            }
        }
    }
}
