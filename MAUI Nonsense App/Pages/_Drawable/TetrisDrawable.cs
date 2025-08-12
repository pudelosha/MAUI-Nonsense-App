using MAUI_Nonsense_App.ViewModels;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages._Drawable;

public class TetrisDrawable : IDrawable
{
    private readonly TetrisViewModel _vm;
    private readonly Color[] _colors =
    {
        Colors.Transparent,
        Colors.Cyan, Colors.Yellow, Colors.Blue, Colors.Orange,
        Colors.Green, Colors.Red, Colors.Purple
    };

    public TetrisDrawable(TetrisViewModel vm) => _vm = vm;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float cell = _vm.CellPx;
        float boardW = _vm.Cols * cell;
        float boardH = _vm.Rows * cell;

        // Center the board inside the canvas
        float ox = dirtyRect.X + (dirtyRect.Width - boardW) / 2f;
        float oy = dirtyRect.Y + (dirtyRect.Height - boardH) / 2f;

        DrawBoardBackground(canvas, ox, oy, boardW, boardH);
        DrawPlacedBlocks(canvas, ox, oy, cell);
        DrawActivePiece(canvas, ox, oy, cell);
        DrawGrid(canvas, ox, oy, cell, boardW, boardH);
        DrawOverlay(canvas, ox, oy, boardW, boardH);
    }

    private void DrawBoardBackground(ICanvas canvas, float ox, float oy, float w, float h)
    {
        // Light-gray playfield – no rounded corners
        canvas.FillColor = Colors.LightGray;
        canvas.FillRectangle(ox, oy, w, h);
    }

    private void DrawPlacedBlocks(ICanvas canvas, float ox, float oy, float cell)
    {
        if (_vm.Board == null) return;

        for (int y = 0; y < _vm.Rows; y++)
        {
            for (int x = 0; x < _vm.Cols; x++)
            {
                int colorIndex = _vm.Board[y, x];
                if (colorIndex == 0) continue;

                canvas.FillColor = _colors[colorIndex];
                canvas.FillRectangle(ox + x * cell, oy + y * cell, cell - 1, cell - 1);
            }
        }
    }

    private void DrawActivePiece(ICanvas canvas, float ox, float oy, float cell)
    {
        if (_vm.Board == null) return;

        canvas.FillColor = _colors[_vm.CurrentColor];
        foreach (var (x, y) in _vm.CurrentShape)
        {
            float px = ox + (_vm.CurrentX + x) * cell;
            float py = oy + (_vm.CurrentY + y) * cell;
            canvas.FillRectangle(px, py, cell - 1, cell - 1);
        }
    }

    private void DrawGrid(ICanvas canvas, float ox, float oy, float cell, float w, float h)
    {
        // Very subtle grid to make it look crisp
        canvas.StrokeColor = new Color(0, 0, 0, 0.08f);
        canvas.StrokeSize = 1;

        for (int x = 0; x <= _vm.Cols; x++)
            canvas.DrawLine(ox + x * cell, oy, ox + x * cell, oy + h);

        for (int y = 0; y <= _vm.Rows; y++)
            canvas.DrawLine(ox, oy + y * cell, ox + w, oy + y * cell);
    }

    private void DrawOverlay(ICanvas canvas, float ox, float oy, float w, float h)
    {
        if (_vm.State == TetrisState.Running || _vm.State == TetrisState.Ready) return;

        var text = _vm.State switch
        {
            TetrisState.Paused => "Paused",
            TetrisState.GameOver => "Game Over",
            _ => ""
        };
        if (string.IsNullOrEmpty(text)) return;

        canvas.FontColor = Colors.White;
        canvas.FontSize = 24;
        canvas.DrawString(text, new RectF(ox, oy, w, h),
                          HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
