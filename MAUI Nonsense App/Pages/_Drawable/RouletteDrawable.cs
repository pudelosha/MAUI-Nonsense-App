using MAUI_Nonsense_App.Models;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages._Drawable;

public class RouletteDrawable : IDrawable
{
    private readonly RouletteViewModel _viewModel;

    public RouletteDrawable(RouletteViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        var center = new PointF(rect.Width / 2, rect.Height / 2);
        float outerRadius = Math.Min(rect.Width, rect.Height) / 2 - 10;
        float innerRadius = outerRadius * 0.7f;
        float sliceAngle = 360f / _viewModel.Slots.Count;

        canvas.SaveState();
        canvas.Translate(center.X, center.Y);

        for (int i = 0; i < _viewModel.Slots.Count; i++)
        {
            float startAngle = i * sliceAngle;

            // Draw arc
            canvas.FillColor = _viewModel.Slots[i].Color;
            canvas.FillArc(-outerRadius, -outerRadius, outerRadius * 2, outerRadius * 2,
                           startAngle, sliceAngle, true);

            // Draw text
            canvas.SaveState();
            float labelAngle = startAngle + sliceAngle / 2;
            canvas.Rotate(labelAngle);
            canvas.Translate(0, -outerRadius * 0.85f);
            canvas.FontSize = 12;
            canvas.FontColor = Colors.White;

            var label = _viewModel.Slots[i].Label;
            var size = canvas.GetStringSize(label, null, 12);

            canvas.DrawString(
                label,
                -size.Width / 2,
                -size.Height / 2,
                size.Width,
                size.Height,
                HorizontalAlignment.Center,
                VerticalAlignment.Center
            );

            canvas.RestoreState();
        }

        // Draw the white ball
        float ballAngleRad = (float)(_viewModel.BallAngle * Math.PI / 180);
        float ballX = innerRadius * (float)Math.Cos(ballAngleRad);
        float ballY = innerRadius * (float)Math.Sin(ballAngleRad);

        canvas.FillColor = Colors.White;
        canvas.FillCircle(ballX, ballY, 8);

        canvas.RestoreState();
    }
}
