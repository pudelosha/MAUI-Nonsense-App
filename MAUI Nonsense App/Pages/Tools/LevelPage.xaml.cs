using Microsoft.Maui.Graphics;
using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages.Tools;

public partial class LevelPage : ContentPage
{
    private readonly LevelViewModel _viewModel;

    public LevelPage(LevelViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        BindingContext = _viewModel;

        _viewModel.PropertyChanged += (_, __) => UpdateUI();

        // Wire up drawables that read _viewModel values
        LevelCanvas.Drawable = new LevelDrawable(() => ((float)_viewModel.Roll / 90f, (float)_viewModel.Pitch / 90f));
        HorizontalBubbleCanvas.Drawable = new HorizontalBubbleDrawable(() => (float)_viewModel.Roll / 90f);
        VerticalBubbleCanvas.Drawable = new VerticalBubbleDrawable(() => (float)_viewModel.Pitch / 90f);

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

        // Start the sensor service when the page appears
        await _viewModel.StartAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        // Stop the sensor service when the page disappears
        await _viewModel.StopAsync();
    }

    private void UpdateUI()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ReadingLabel.Text = $"X: {_viewModel.Roll:F1}°   Y: {_viewModel.Pitch:F1}°";
        });
    }

    class LevelDrawable : IDrawable
    {
        private readonly Func<(float x, float y)> _getTilt;

        public LevelDrawable(Func<(float x, float y)> getTilt) => _getTilt = getTilt;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float centerX = dirtyRect.Center.X;
            float centerY = dirtyRect.Center.Y;
            float radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2 - 10;

            // Background
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);

            // Circle background
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 3;
            canvas.DrawCircle(centerX, centerY, radius);

            canvas.FillColor = Colors.LimeGreen;
            canvas.FillCircle(centerX, centerY, radius);

            // Crosshairs
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawLine(centerX - radius, centerY, centerX + radius, centerY);
            canvas.DrawLine(centerX, centerY - radius, centerX, centerY + radius);

            var (x, y) = _getTilt();
            float offsetX = -x * (radius - 20);
            float offsetY = y * (radius - 20);

            // Moving inner bubble
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

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawRectangle(dirtyRect);

            canvas.FillColor = Colors.LimeGreen;
            canvas.FillRectangle(dirtyRect);

            float bubbleRadius = dirtyRect.Height / 2 - 4;
            float trackLength = dirtyRect.Width - bubbleRadius * 2;

            float offset = _getX() * (trackLength / 2);

            float centerX = dirtyRect.Center.X;

            // Draw two bars with spacing wide enough to fit the bubble
            float barSpacing = bubbleRadius * 2.5f; // adjust as needed
            float barHeight = dirtyRect.Height - 4;
            float barWidth = 2;

            canvas.FillColor = Colors.Black;
            canvas.FillRectangle(centerX - barSpacing / 2 - barWidth / 2, dirtyRect.Y + 2, barWidth, barHeight);
            canvas.FillRectangle(centerX + barSpacing / 2 - barWidth / 2, dirtyRect.Y + 2, barWidth, barHeight);

            // Egg-shaped bubble
            canvas.FillColor = Colors.LightGreen.WithAlpha(0.7f);
            RectF bubbleRect = new(
                centerX + offset - bubbleRadius * 1.2f,
                dirtyRect.Center.Y - bubbleRadius,
                bubbleRadius * 2.4f,
                bubbleRadius * 2
            );
            canvas.FillEllipse(bubbleRect);
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

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawRectangle(dirtyRect);

            canvas.FillColor = Colors.LimeGreen;
            canvas.FillRectangle(dirtyRect);

            float bubbleRadius = dirtyRect.Width / 2 - 4;
            float trackLength = dirtyRect.Height - bubbleRadius * 2;

            float offset = _getY() * (trackLength / 2);

            float centerY = dirtyRect.Center.Y;

            // Draw two bars with spacing wide enough to fit the bubble
            float barSpacing = bubbleRadius * 2.5f; // adjust as needed
            float barWidth = dirtyRect.Width - 4;
            float barHeight = 2;

            canvas.FillColor = Colors.Black;
            canvas.FillRectangle(dirtyRect.X + 2, centerY - barSpacing / 2 - barHeight / 2, barWidth, barHeight);
            canvas.FillRectangle(dirtyRect.X + 2, centerY + barSpacing / 2 - barHeight / 2, barWidth, barHeight);

            // Egg-shaped bubble
            canvas.FillColor = Colors.LightGreen.WithAlpha(0.7f);
            RectF bubbleRect = new(
                dirtyRect.Center.X - bubbleRadius,
                centerY + offset - bubbleRadius * 1.2f,
                bubbleRadius * 2,
                bubbleRadius * 2.4f
            );
            canvas.FillEllipse(bubbleRect);
        }
    }
}
