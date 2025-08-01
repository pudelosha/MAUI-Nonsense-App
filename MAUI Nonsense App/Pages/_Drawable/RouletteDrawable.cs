using MAUI_Nonsense_App.ViewModels;
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
        float fullRadius = Math.Min(rect.Width, rect.Height) / 2 - 10;

        float outerRadius = fullRadius;
        float innerRadius = fullRadius * 0.75f;
        float innerRingInnerRadius = fullRadius * 0.45f;

        float sliceAngle = 360f / _viewModel.Slots.Count;

        canvas.SaveState();
        canvas.Translate(center.X, center.Y);

        for (int i = 0; i < _viewModel.Slots.Count; i++)
        {
            float startAngle = i * sliceAngle - 90 - (sliceAngle / 2); // Center each slice

            var slot = _viewModel.Slots[i];

            // OUTER RING SLICE
            var outerPath = CreateRingSlice(startAngle, sliceAngle, innerRadius, outerRadius);
            canvas.FillColor = slot.Color;
            canvas.FillPath(outerPath);

            // INNER RING SLICE
            var innerPath = CreateRingSlice(startAngle, sliceAngle, innerRingInnerRadius, innerRadius);
            canvas.FillColor = slot.Color;
            canvas.FillPath(innerPath);

            // SLOT LABEL
            float midAngle = startAngle + sliceAngle / 2;
            float labelRadius = (outerRadius + innerRadius) / 2;
            float labelX = labelRadius * (float)Math.Cos(DegreesToRadians(midAngle));
            float labelY = labelRadius * (float)Math.Sin(DegreesToRadians(midAngle));

            canvas.FontSize = 12;
            canvas.FontColor = Colors.White;
            var size = canvas.GetStringSize(slot.Label, null, 12);

            canvas.DrawString(
                slot.Label,
                labelX - size.Width / 2,
                labelY - size.Height / 2,
                size.Width,
                size.Height,
                HorizontalAlignment.Center,
                VerticalAlignment.Center
            );
        }

        // OPTIONAL: center filler
        canvas.FillColor = Colors.DarkGreen;
        canvas.FillCircle(0, 0, innerRingInnerRadius * 0.8f);

        // WHITE BALL SPINNING (adjusted for centered slices)
        float ballOffsetAngle = -90f - (sliceAngle / 2); // aligns with centered slices
        float ballVisualAngle = _viewModel.BallAngle + ballOffsetAngle;
        float ballAngleRad = DegreesToRadians(ballVisualAngle);
        float ballRadius = outerRadius * 0.75f; // close to outer edge, but inside text
        float ballX = ballRadius * (float)Math.Cos(ballAngleRad);
        float ballY = ballRadius * (float)Math.Sin(ballAngleRad);

        canvas.FillColor = Colors.White;
        canvas.FillCircle(ballX, ballY, 8);

        canvas.RestoreState();
    }

    private static PathF CreateRingSlice(float startAngle, float sweepAngle, float innerR, float outerR)
    {
        var path = new PathF();

        for (int j = 0; j <= 10; j++)
        {
            float angle = startAngle + j * (sweepAngle / 10);
            float rad = DegreesToRadians(angle);
            float x = outerR * (float)Math.Cos(rad);
            float y = outerR * (float)Math.Sin(rad);
            if (j == 0) path.MoveTo(x, y);
            else path.LineTo(x, y);
        }

        for (int j = 10; j >= 0; j--)
        {
            float angle = startAngle + j * (sweepAngle / 10);
            float rad = DegreesToRadians(angle);
            float x = innerR * (float)Math.Cos(rad);
            float y = innerR * (float)Math.Sin(rad);
            path.LineTo(x, y);
        }

        path.Close();
        return path;
    }

    private static float DegreesToRadians(float degrees) =>
        (float)(Math.PI / 180 * degrees);
}
