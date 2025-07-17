using MAUI_Nonsense_App.Models;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages.Tools;

public partial class LevelPage : ContentPage
{
    private readonly LevelViewModel _viewModel;

    public LevelPage(LevelViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;

        LevelCanvas.Drawable = new LevelDrawable(() => ((float)_viewModel.TiltX, (float)_viewModel.TiltY));
        HorizontalBubbleCanvas.Drawable = new HorizontalBubbleDrawable(() => (float)_viewModel.TiltX);
        VerticalBubbleCanvas.Drawable = new VerticalBubbleDrawable(() => (float)_viewModel.TiltY);

        Device.StartTimer(TimeSpan.FromMilliseconds(30), () =>
        {
            LevelCanvas.Invalidate();
            HorizontalBubbleCanvas.Invalidate();
            VerticalBubbleCanvas.Invalidate();
            return true;
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.StartAsync();
    }

    protected override async void OnDisappearing()
    {
        await _viewModel.StopAsync();
        base.OnDisappearing();
    }

    class LevelDrawable : IDrawable
    {
        private readonly Func<(float x, float y)> _getTilt;

        public LevelDrawable(Func<(float x, float y)> getTilt) => _getTilt = getTilt;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);

            float centerX = dirtyRect.Center.X;
            float centerY = dirtyRect.Center.Y;
            float radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2 - 10;

            // outer green bubble
            canvas.FillColor = Colors.LimeGreen;
            canvas.FillCircle(centerX, centerY, radius);

            // crosshairs
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawLine(centerX - radius, centerY, centerX + radius, centerY);
            canvas.DrawLine(centerX, centerY - radius, centerX, centerY + radius);

            // static small circle
            canvas.StrokeColor = Colors.DarkGreen;
            canvas.StrokeSize = 2;
            canvas.DrawCircle(centerX, centerY, radius / 4);

            // moving inner bubble
            var (x, y) = _getTilt();
            float offsetX = -x * (radius - 20);  // invert horizontal
            float offsetY = y * (radius - 20);

            canvas.FillColor = Colors.LightGreen.WithAlpha(0.7f);
            canvas.FillCircle(centerX + offsetX, centerY + offsetY, radius / 8);
        }
    }

    class HorizontalBubbleDrawable : IDrawable
    {
        private readonly Func<float> _getX;

        public HorizontalBubbleDrawable(Func<float> getX) => _getX = getX;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);

            canvas.FillColor = Colors.LimeGreen;
            canvas.FillRectangle(dirtyRect);

            float bubbleRadius = dirtyRect.Height / 2 - 4;
            float trackLength = dirtyRect.Width - bubbleRadius * 2;

            // invert the offset so bubble moves opposite to tilt
            float offset = -_getX() * (trackLength / 2);

            canvas.FillColor = Colors.LightGreen.WithAlpha(0.7f);
            canvas.FillCircle(dirtyRect.Center.X + offset, dirtyRect.Center.Y, bubbleRadius);
        }
    }

    class VerticalBubbleDrawable : IDrawable
    {
        private readonly Func<float> _getY;

        public VerticalBubbleDrawable(Func<float> getY) => _getY = getY;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);

            canvas.FillColor = Colors.LimeGreen;
            canvas.FillRectangle(dirtyRect);

            float bubbleRadius = dirtyRect.Width / 2 - 4;
            float trackLength = dirtyRect.Height - bubbleRadius * 2;

            float offset = _getY() * (trackLength / 2);

            canvas.FillColor = Colors.LightGreen.WithAlpha(0.7f);
            canvas.FillCircle(dirtyRect.Center.X, dirtyRect.Center.Y + offset, bubbleRadius);
        }
    }
}
