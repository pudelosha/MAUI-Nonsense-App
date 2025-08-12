using MAUI_Nonsense_App.ViewModels;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages._Drawable;

public class SnakeDrawable : IDrawable
{
    private readonly SnakeViewModel _vm;

    public SnakeDrawable(SnakeViewModel vm) => _vm = vm;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        DrawGrid(canvas);
        DrawFruit(canvas);
        DrawSnake(canvas);
        DrawOverlay(canvas);
    }

    private void DrawGrid(ICanvas canvas)
    {
        var cell = _vm.CellPx;
        canvas.StrokeColor = new Color(1f, 1f, 1f, 0.08f);
        canvas.StrokeSize = 1;

        for (int x = 0; x <= _vm.Cols; x++)
        {
            float px = x * cell;
            canvas.DrawLine(px, 0, px, _vm.Rows * cell);
        }
        for (int y = 0; y <= _vm.Rows; y++)
        {
            float py = y * cell;
            canvas.DrawLine(0, py, _vm.Cols * cell, py);
        }
    }

    private void DrawFruit(ICanvas canvas)
    {
        var cell = _vm.CellPx;
        float cx = (float)(_vm.FruitCell.X * cell + cell / 2.0);
        float cy = (float)(_vm.FruitCell.Y * cell + cell / 2.0);

        canvas.FillColor = Colors.OrangeRed;
        canvas.FillCircle(cx, cy, cell * 0.35f);

        canvas.FillColor = Colors.ForestGreen;
        canvas.FillRoundedRectangle(
            cx - cell * 0.08f,
            cy - cell * 0.55f,
            cell * 0.16f,
            cell * 0.20f,
            2f);
    }

    private void DrawSnake(ICanvas canvas)
    {
        var cell = _vm.CellPx;
        var radius = cell * 0.3f;

        // Body
        canvas.FillColor = new Color(0.2f, 0.9f, 0.4f);
        foreach (var seg in _vm.SnakeCells)
        {
            var rect = new RectF(
                (float)(seg.X * cell + 1),
                (float)(seg.Y * cell + 1),
                cell - 2,
                cell - 2);
            canvas.FillRoundedRectangle(rect, 6);
        }

        // Head
        var head = _vm.SnakeCells.Last();
        var headRect = new RectF(
            (float)(head.X * cell + 1),
            (float)(head.Y * cell + 1),
            cell - 2,
            cell - 2);

        canvas.FillColor = new Color(0.1f, 0.8f, 0.35f);
        canvas.FillRoundedRectangle(headRect, 8);

        // Eyes
        canvas.FillColor = Colors.White;
        var (ex1, ey1, ex2, ey2) = Eyes(headRect, _vm.Heading);
        canvas.FillCircle(ex1, ey1, radius * 0.25f);
        canvas.FillCircle(ex2, ey2, radius * 0.25f);

        canvas.FillColor = Colors.Black;
        canvas.FillCircle(ex1, ey1, radius * 0.12f);
        canvas.FillCircle(ex2, ey2, radius * 0.12f);
    }

    private (float, float, float, float) Eyes(RectF head, Direction dir)
    {
        float inset = head.Width * 0.25f;
        return dir switch
        {
            Direction.Up => (head.X + inset, head.Y + head.Height * 0.30f,
                                head.Right - inset, head.Y + head.Height * 0.30f),
            Direction.Down => (head.X + inset, head.Bottom - head.Height * 0.30f,
                                head.Right - inset, head.Bottom - head.Height * 0.30f),
            Direction.Left => (head.X + head.Width * 0.30f, head.Y + inset,
                                head.X + head.Width * 0.30f, head.Bottom - inset),
            Direction.Right => (head.Right - head.Width * 0.30f, head.Y + inset,
                                head.Right - head.Width * 0.30f, head.Bottom - inset),
            _ => (head.Center.X - 4, head.Center.Y - 4, head.Center.X + 4, head.Center.Y + 4)
        };
    }

    private void DrawOverlay(ICanvas canvas)
    {
        // No "Start" overlay. Only show for Paused/GameOver.
        if (_vm.State == GameState.Running || _vm.State == GameState.Ready)
            return;

        var text = _vm.State switch
        {
            GameState.Paused => "Paused",
            GameState.GameOver => "Game Over",
            _ => ""
        };
        if (string.IsNullOrEmpty(text)) return;

        canvas.FontColor = Colors.White;
        canvas.FontSize = 24;

        var bounds = new RectF(0, 0, _vm.Cols * _vm.CellPx, _vm.Rows * _vm.CellPx);
        canvas.DrawString(text, bounds, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
