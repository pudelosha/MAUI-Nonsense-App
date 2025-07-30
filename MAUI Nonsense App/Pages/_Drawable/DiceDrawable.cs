using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages._Drawable
{
    public class DiceDrawable : IDrawable
    {
        private readonly DiceViewModel _viewModel;

        public DiceDrawable(DiceViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float boxSize = 80;
            float spacing = 30;
            float startX = (dirtyRect.Width - (_viewModel.DiceCount * (boxSize + spacing) - spacing)) / 2;
            float y = dirtyRect.Height / 2 - boxSize / 2;

            for (int i = 0; i < _viewModel.DiceCount; i++)
            {
                int value = _viewModel.RollResults.Count > i ? _viewModel.RollResults[i] : 1;
                float x = startX + i * (boxSize + spacing);
                DrawDice(canvas, new RectF(x, y, boxSize, boxSize), value);
            }
        }

        private void DrawDice(ICanvas canvas, RectF rect, int value)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRoundedRectangle(rect, 10);
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawRoundedRectangle(rect, 10);

            canvas.FillColor = Colors.Black;
            float w = rect.Width;
            float h = rect.Height;

            void Dot(float dx, float dy)
            {
                canvas.FillCircle(rect.X + dx * w, rect.Y + dy * h, w * 0.07f);
            }

            if (value == 1 || value == 3 || value == 5) Dot(0.5f, 0.5f);
            if (value >= 2) { Dot(0.25f, 0.25f); Dot(0.75f, 0.75f); }
            if (value >= 4) { Dot(0.25f, 0.75f); Dot(0.75f, 0.25f); }
            if (value == 6) { Dot(0.25f, 0.5f); Dot(0.75f, 0.5f); }
        }
    }
}
