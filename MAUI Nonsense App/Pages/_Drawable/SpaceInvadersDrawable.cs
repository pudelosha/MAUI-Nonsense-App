using MAUI_Nonsense_App.ViewModels;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages._Drawable;

public class SpaceInvadersDrawable : IDrawable
{
    private readonly SpaceInvadersViewModel _vm;
    public SpaceInvadersDrawable(SpaceInvadersViewModel vm) => _vm = vm;

    // Simple pixel-art alien (8 x 6)
    // 1 = filled pixel, 0 = empty
    private static readonly byte[,] AlienMask = new byte[,]
    {
        {0,1,1,0,0,1,1,0},
        {1,1,1,1,1,1,1,1},
        {1,1,0,1,1,0,1,1},
        {1,1,1,1,1,1,1,1},
        {0,1,1,0,0,1,1,0},
        {1,0,0,1,1,0,0,1},
    };

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var pf = _vm.Playfield;

        // Inner board background
        canvas.FillColor = Colors.LightGray;
        canvas.FillRectangle(pf);

        DrawAliens(canvas);
        DrawShip(canvas);
        DrawShots(canvas);
        DrawOverlay(canvas, pf);
    }

    private void DrawAliens(ICanvas canvas)
    {
        foreach (var (rect, alive, row) in _vm.Aliens)
        {
            if (!alive) continue;

            // Row-based color
            var color = row switch
            {
                0 => Colors.MediumPurple,
                1 => Colors.CornflowerBlue,
                2 => Colors.MediumSeaGreen,
                3 => Colors.Goldenrod,
                _ => Colors.OrangeRed
            };

            DrawAlienIcon(canvas, rect, color);
        }
    }

    private void DrawAlienIcon(ICanvas canvas, RectF rect, Color color)
    {
        int cols = AlienMask.GetLength(0);
        int rows = AlienMask.GetLength(1);

        // Slight inset so pixels don't touch the rect edges
        float pad = rect.Width * 0.06f;
        var r = new RectF(rect.X + pad, rect.Y + pad, rect.Width - 2 * pad, rect.Height - 2 * pad);

        float cw = r.Width / cols;
        float ch = r.Height / rows;
        float pixelInset = Math.Min(cw, ch) * 0.15f;

        canvas.FillColor = color;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                if (AlienMask[x, y] == 0) continue;
                float px = r.X + x * cw + pixelInset;
                float py = r.Y + y * ch + pixelInset;
                float pw = cw - 2 * pixelInset;
                float ph = ch - 2 * pixelInset;
                canvas.FillRectangle(px, py, pw, ph);
            }
        }

        // Soft outline
        canvas.StrokeColor = new Color(0, 0, 0, 0.15f);
        canvas.StrokeSize = 1;
        canvas.DrawRectangle(rect);
    }

    private void DrawShip(ICanvas canvas)
    {
        var r = _vm.ShipRect;
        canvas.FillColor = Colors.DarkSlateGray;
        canvas.FillRectangle(r);

        // small turret
        canvas.FillColor = Colors.Black;
        var turret = new RectF(r.Center.X - r.Width * 0.07f, r.Top - r.Height * 0.25f, r.Width * 0.14f, r.Height * 0.25f);
        canvas.FillRectangle(turret);
    }

    private void DrawShots(ICanvas canvas)
    {
        foreach (var (rect, fromPlayer) in _vm.Shots)
        {
            canvas.FillColor = fromPlayer ? Colors.White : Colors.Black;
            canvas.FillRectangle(rect);
        }
    }

    private void DrawOverlay(ICanvas canvas, RectF pf)
    {
        if (_vm.State == SIState.Running || _vm.State == SIState.Ready) return;

        string text = _vm.State switch
        {
            SIState.Paused => "Paused — drag/tap or Start to resume",
            SIState.GameOver => "Game Over",
            _ => ""
        };
        if (string.IsNullOrEmpty(text)) return;

        canvas.FontColor = Colors.White;
        canvas.FontSize = 18;
        canvas.DrawString(text, pf, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
