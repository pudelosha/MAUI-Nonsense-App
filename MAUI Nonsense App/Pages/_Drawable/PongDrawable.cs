using MAUI_Nonsense_App.ViewModels;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages._Drawable;

public class PongDrawable : IDrawable
{
    private readonly PongViewModel _vm;
    public PongDrawable(PongViewModel vm) => _vm = vm;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var pf = _vm.Playfield;

        // Inner board
        canvas.FillColor = Colors.LightGray;
        canvas.FillRectangle(pf);

        // Center dashed line
        canvas.StrokeColor = new Color(0, 0, 0, 0.2f);
        canvas.StrokeSize = 2f;
        float dash = 10f, gap = 10f, x = pf.Center.X, y = pf.Top;
        while (y < pf.Bottom)
        {
            canvas.DrawLine(x, y, x, Math.Min(y + dash, pf.Bottom));
            y += dash + gap;
        }

        // Paddles
        canvas.FillColor = Colors.DimGray;
        canvas.FillRectangle(_vm.PlayerRect);
        canvas.FillRectangle(_vm.CpuRect);

        // Ball
        var (bx, by) = _vm.Ball;
        canvas.FillColor = Colors.White;
        canvas.FillCircle(bx, by, _vm.BallRadius);
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 1f;
        canvas.DrawCircle(bx, by, _vm.BallRadius);

        // Overlays
        if (_vm.State == PongState.RoundPaused)
        {
            canvas.FontColor = Colors.White;
            canvas.FontSize = 20;
            canvas.DrawString("Tap/drag or Start to serve",
                              pf, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
        else if (_vm.State == PongState.GameOver)
        {
            canvas.FontColor = Colors.White;
            canvas.FontSize = 20;
            canvas.DrawString("Game Over",
                              pf, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
