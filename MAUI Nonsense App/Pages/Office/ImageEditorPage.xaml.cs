using System;
using System.IO;
using System.Linq; // Any(), First()
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Devices; // DeviceDisplay
using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages.Office;

public partial class ImageEditorPage : ContentPage
{
    private readonly ImagePageModel _imagePage;

    private readonly FrameDrawable _drawable;
    private RectF _imageRectOnCanvas;
    private Size _bitmapSize;
    private int? _draggingIndex = null;
    private const float HandleRadiusDp = 10f;
    private const float HitRadiusDp = 24f;

    public ImageEditorPage(ImagePageModel imagePage)
    {
        InitializeComponent();
        _imagePage = imagePage;

        _drawable = new FrameDrawable();
        Overlay.Drawable = _drawable;

        Overlay.StartInteraction += OverlayOnStartInteraction;
        Overlay.DragInteraction += OverlayOnDragInteraction;
        Overlay.EndInteraction += OverlayOnEndInteraction;

        Overlay.SizeChanged += (_, __) => RecomputeImageRectAndRedraw();
        EditableImage.SizeChanged += (_, __) => RecomputeImageRectAndRedraw();

        LoadImage();
    }

    private void LoadImage()
    {
        if (!File.Exists(_imagePage.FilePath))
            return;

        EditableImage.Source = ImageSource.FromFile(_imagePage.FilePath);

        // Best-effort intrinsic size (not critical on Android because we query the actual drawn rect)
        try
        {
            using var fs = File.OpenRead(_imagePage.FilePath);
            var img = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(fs);
            _bitmapSize = new Size(img.Width, img.Height);
        }
        catch
        {
            _bitmapSize = new Size(1000, 1000);
        }

        // Default to full image unless we have a saved crop
        if (_imagePage.FrameCrop is { } saved)
            _drawable.SetNormalizedCorners(saved);
        else
            _drawable.SetNormalizedCorners(CropQuadNormalized.FullImage);

        RecomputeImageRectAndRedraw();
    }

    private void RecomputeImageRectAndRedraw()
    {
#if ANDROID
        var rectFromAndroid = TryGetAndroidDisplayedImageRectInDips();
        if (rectFromAndroid.HasValue)
        {
            _imageRectOnCanvas = rectFromAndroid.Value;
        }
        else
#endif
        {
            if (Overlay.Width <= 0 || Overlay.Height <= 0 || _bitmapSize.Width <= 0 || _bitmapSize.Height <= 0)
                return;

            // Fallback (non-Android or drawable not ready yet)
            _imageRectOnCanvas = ComputeAspectFitRect(new Size(Overlay.Width, Overlay.Height), _bitmapSize);
        }

        _drawable.ImageRect = _imageRectOnCanvas;
        Overlay.Invalidate();
    }

    private static RectF ComputeAspectFitRect(Size container, Size image)
    {
        var scale = (float)Math.Min(container.Width / image.Width, container.Height / image.Height);
        var w = (float)(image.Width * scale);
        var h = (float)(image.Height * scale);
        var x = (float)((container.Width - w) / 2.0);
        var y = (float)((container.Height - h) / 2.0);
        return new RectF(x, y, w, h);
    }

    private float DpToPixels(float dp) =>
        (float)(dp * DeviceDisplay.MainDisplayInfo.Density);

    #region Interaction

    private void OverlayOnStartInteraction(object? sender, TouchEventArgs e)
    {
        if (!e.Touches.Any()) return;

        var p = e.Touches.First();
        var hitRadius = DpToPixels(HitRadiusDp);

        _draggingIndex = _drawable.HitTestHandle(new PointF((float)p.X, (float)p.Y), hitRadius);
    }

    private void OverlayOnDragInteraction(object? sender, TouchEventArgs e)
    {
        if (_draggingIndex is null || !e.Touches.Any()) return;

        var p = e.Touches.First();
        var newPt = new PointF((float)p.X, (float)p.Y);

        var clamped = new PointF(
            x: Math.Clamp(newPt.X, _imageRectOnCanvas.Left, _imageRectOnCanvas.Right),
            y: Math.Clamp(newPt.Y, _imageRectOnCanvas.Top, _imageRectOnCanvas.Bottom));

        _drawable.MoveHandle(_draggingIndex.Value, clamped);
        Overlay.Invalidate();
    }

    private void OverlayOnEndInteraction(object? sender, TouchEventArgs e)
    {
        _draggingIndex = null;
    }

    #endregion

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var norm = _drawable.GetNormalizedCorners();
        _imagePage.FrameCrop = norm;
        await DisplayAlert("Saved", "Frame coordinates saved.", "OK");
        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

#if ANDROID
    /// <summary>
    /// Reads the exact rectangle where Android's ImageView drew the bitmap (in DIPs).
    /// This accounts for EXIF rotation, scaling, and matrix translations.
    /// </summary>
    private RectF? TryGetAndroidDisplayedImageRectInDips()
    {
        var handler = EditableImage?.Handler;
        if (handler == null) return null;

        var imageView = handler.PlatformView as global::Android.Widget.ImageView;
        var drawable = imageView?.Drawable;
        if (imageView == null || drawable == null) return null;

        // Drawable bounds in source pixels
        var srcRect = new global::Android.Graphics.RectF(0, 0, drawable.IntrinsicWidth, drawable.IntrinsicHeight);

        // Copy ImageMatrix and map to view-space pixels
        var matrix = new global::Android.Graphics.Matrix(imageView.ImageMatrix);
        matrix.MapRect(srcRect);

        // Include view padding (also in pixels)
        srcRect.Offset(imageView.PaddingLeft, imageView.PaddingTop);

        // Convert to MAUI device-independent units (DIPs)
        var density = (float)DeviceDisplay.MainDisplayInfo.Density;
        return new RectF(
            srcRect.Left / density,
            srcRect.Top / density,
            srcRect.Width() / density,
            srcRect.Height() / density
        );
    }
#endif

    // === Overlay drawable ===
    private sealed class FrameDrawable : IDrawable
    {
        // 0=TL, 1=TR, 2=BR, 3=BL
        private readonly PointF[] _canvasPoints = new PointF[4];
        private CropQuadNormalized _normalized = CropQuadNormalized.FullImage;

        public RectF ImageRect { get; set; }
        private const float HandleRadius = 10f;
        private const float LineThickness = 2f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            for (int i = 0; i < 4; i++)
                _canvasPoints[i] = NormalizedToCanvas(GetNorm(i));

            canvas.StrokeColor = Colors.LimeGreen;
            canvas.StrokeSize = LineThickness;
            for (int i = 0; i < 4; i++)
            {
                var a = _canvasPoints[i];
                var b = _canvasPoints[(i + 1) % 4];
                canvas.DrawLine(a, b);
            }

            canvas.FillColor = Colors.LimeGreen;
            foreach (var p in _canvasPoints)
                canvas.FillCircle(p, HandleRadius);
        }

        public void SetNormalizedCorners(CropQuadNormalized quad) => _normalized = quad;
        public CropQuadNormalized GetNormalizedCorners() => _normalized;

        public int? HitTestHandle(PointF pt, float hitRadiusPx)
        {
            for (int i = 0; i < 4; i++)
            {
                var c = _canvasPoints[i];
                var dx = c.X - pt.X;
                var dy = c.Y - pt.Y;
                if ((dx * dx + dy * dy) <= hitRadiusPx * hitRadiusPx)
                    return i;
            }
            return null;
        }

        public void MoveHandle(int index, PointF newCanvasPoint)
        {
            _canvasPoints[index] = newCanvasPoint;
            var norm = CanvasToNormalized(newCanvasPoint);
            SetNorm(index, norm);
        }

        private PointF NormalizedToCanvas(PointD n) =>
            new(
                (float)(ImageRect.Left + n.X * ImageRect.Width),
                (float)(ImageRect.Top + n.Y * ImageRect.Height)
            );

        private PointD CanvasToNormalized(PointF c)
        {
            if (ImageRect.Width <= 0 || ImageRect.Height <= 0)
                return new PointD(0, 0);

            var x = (c.X - ImageRect.Left) / ImageRect.Width;
            var y = (c.Y - ImageRect.Top) / ImageRect.Height;

            return new PointD(
                Math.Clamp(x, 0f, 1f),
                Math.Clamp(y, 0f, 1f)
            );
        }

        private PointD GetNorm(int idx) => idx switch
        {
            0 => _normalized.TL,
            1 => _normalized.TR,
            2 => _normalized.BR,
            3 => _normalized.BL,
            _ => _normalized.TL
        };

        private void SetNorm(int idx, PointD value)
        {
            switch (idx)
            {
                case 0: _normalized = _normalized with { TL = value }; break;
                case 1: _normalized = _normalized with { TR = value }; break;
                case 2: _normalized = _normalized with { BR = value }; break;
                case 3: _normalized = _normalized with { BL = value }; break;
            }
        }
    }
}
