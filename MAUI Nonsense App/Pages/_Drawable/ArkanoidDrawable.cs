using MAUI_Nonsense_App.ViewModels;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages._Drawable;

public class ArkanoidDrawable : IDrawable
{
    private readonly ArkanoidViewModel _vm;
    public ArkanoidDrawable(ArkanoidViewModel vm) => _vm = vm;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Light gray inner playfield (square corners)
        var pf = _vm.Playfield;
        canvas.FillColor = Colors.LightGray;
        canvas.FillRectangle(pf);

        DrawBricks(canvas, pf);
        DrawPaddle(canvas);
        DrawBall(canvas);
        DrawOverlay(canvas, pf);
    }

    private void DrawBricks(ICanvas canvas, RectF pf)
    {
        foreach (var (rect, color, active) in _vm.Bricks)
        {
            if (!active) continue;
            canvas.FillColor = color;
            canvas.FillRectangle(rect);
            canvas.StrokeColor = new Color(0, 0, 0, 0.1f);
            canvas.StrokeSize = 1;
            canvas.DrawRectangle(rect);
        }
    }

    private void DrawPaddle(ICanvas canvas)
    {
        var r = _vm.PaddleRect;
        canvas.FillColor = Colors.DimGray;
        canvas.FillRectangle(r);
    }

    private void DrawBall(ICanvas canvas)
    {
        var (x, y) = _vm.Ball;
        canvas.FillColor = Colors.White;
        canvas.FillCircle(x, y, _vm.BallRadius);
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 1;
        canvas.DrawCircle(x, y, _vm.BallRadius);
    }

    private void DrawOverlay(ICanvas canvas, RectF pf)
    {
        if (_vm.State == ArkanoidState.Running || _vm.State == ArkanoidState.Ready)
            return;

        string text = _vm.State switch
        {
            ArkanoidState.Paused => "Paused",
            ArkanoidState.GameOver => "Game Over",
            _ => ""
        };
        if (string.IsNullOrEmpty(text)) return;

        canvas.FontColor = Colors.White;
        canvas.FontSize = 24;
        canvas.DrawString(text, pf, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
