using MAUI_Nonsense_App.ViewModels;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages._Drawable;

public class Game2048Drawable : IDrawable
{
    private readonly Game2048ViewModel _vm;

    public Game2048Drawable(Game2048ViewModel vm) => _vm = vm;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        DrawBoardBackground(canvas);
        DrawGrid(canvas);
        DrawTiles(canvas);
        DrawOverlay(canvas);
    }

    private void DrawBoardBackground(ICanvas canvas)
    {
        var r = _vm.Playfield;
        canvas.FillColor = Colors.LightGray;
        canvas.FillRectangle(r);
    }

    private void DrawGrid(ICanvas canvas)
    {
        var r = _vm.Playfield;
        float cell = _vm.CellPx;
        float gap = _vm.Gap;

        // Empty cell slots (slightly darker)
        canvas.FillColor = new Color(0f, 0f, 0f, 0.05f);
        for (int y = 0; y < _vm.Size; y++)
        {
            for (int x = 0; x < _vm.Size; x++)
            {
                float px = r.X + gap + x * (cell + gap);
                float py = r.Y + gap + y * (cell + gap);
                canvas.FillRectangle(px, py, cell, cell);
            }
        }
    }

    private void DrawTiles(ICanvas canvas)
    {
        if (_vm.Board == null) return;

        var r = _vm.Playfield;
        float cell = _vm.CellPx;
        float gap = _vm.Gap;

        for (int y = 0; y < _vm.Size; y++)
        {
            for (int x = 0; x < _vm.Size; x++)
            {
                int v = _vm.Board[y, x];
                if (v == 0) continue;

                float px = r.X + gap + x * (cell + gap);
                float py = r.Y + gap + y * (cell + gap);

                canvas.FillColor = TileColor(v);
                canvas.FillRectangle(px, py, cell, cell);

                // Value text
                canvas.FontSize = Math.Max(14, cell * 0.42f);
                canvas.FontColor = v <= 4 ? new Color(0.33f, 0.29f, 0.25f) : Colors.White;

                canvas.DrawString(
                    v.ToString(),
                    new RectF(px, py, cell, cell),
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center);
            }
        }
    }

    private void DrawOverlay(ICanvas canvas)
    {
        if (_vm.State == Game2048State.Running || _vm.State == Game2048State.Ready) return;

        var r = _vm.Playfield;
        canvas.FontColor = Colors.White;
        canvas.FontSize = 24;
        canvas.DrawString("Game Over", r, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private static Color TileColor(int v) => v switch
    {
        2 => Color.FromArgb("#EEE4DA"),
        4 => Color.FromArgb("#EDE0C8"),
        8 => Color.FromArgb("#F2B179"),
        16 => Color.FromArgb("#F59563"),
        32 => Color.FromArgb("#F67C5F"),
        64 => Color.FromArgb("#F65E3B"),
        128 => Color.FromArgb("#EDCF72"),
        256 => Color.FromArgb("#EDCC61"),
        512 => Color.FromArgb("#EDC850"),
        1024 => Color.FromArgb("#EDC53F"),
        2048 => Color.FromArgb("#EDC22E"),
        _ => Color.FromArgb("#3C3A32") // 4096+
    };
}
