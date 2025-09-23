using System;
using System.IO;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages.Office;

public partial class ImageEditorPage : ContentPage
{
    private readonly ImagePageModel _imagePage;

    private readonly FrameDrawable _drawable;
    private RectF _imageRectOnCanvas;  // where the bitmap draws inside the overlay
    private RectF _dragClampRect;      // enlarged rect to allow dragging outside
    private Size _bitmapSize;
    private int? _draggingIndex;

    // UI (dp)
    private const float HandleRadiusDp = 6f;  // visual size (was 10–12)
    private const float HitRadiusDp = 50f;  // touch halo (make clearly bigger than visual)
    private const float DragOutsetDp = 32f;  // allow more “outside the photo” movement

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
        if (!File.Exists(_imagePage.FilePath)) return;

        EditableImage.Source = ImageSource.FromFile(_imagePage.FilePath);

        if (_imagePage.OriginalPixelWidth > 0 && _imagePage.OriginalPixelHeight > 0)
        {
            _bitmapSize = new Size(_imagePage.OriginalPixelWidth, _imagePage.OriginalPixelHeight);
        }
        else
        {
            try
            {
                using var fs = File.OpenRead(_imagePage.FilePath);
#if ANDROID || IOS || MACCATALYST || WINDOWS
                var img = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(fs);
                _bitmapSize = new Size(img.Width, img.Height);
#else
                _bitmapSize = new Size(1000, 1000);
#endif
            }
            catch
            {
                _bitmapSize = new Size(1000, 1000);
            }
        }

        _drawable.SetNormalizedCorners(_imagePage.FrameCrop ?? CropQuadNormalized.FullImage);
        RecomputeImageRectAndRedraw();
    }

    private void RecomputeImageRectAndRedraw()
    {
#if ANDROID
        var rectFromAndroid = TryGetAndroidDisplayedImageRectInDips();
        _imageRectOnCanvas = rectFromAndroid ?? ComputeAspectFitRect(new Size(Overlay.Width, Overlay.Height), _bitmapSize);
#else
        _imageRectOnCanvas = ComputeAspectFitRect(new Size(Overlay.Width, Overlay.Height), _bitmapSize);
#endif
        if (_imageRectOnCanvas.Width <= 0 || _imageRectOnCanvas.Height <= 0) return;

        // Allow going a bit outside the image, but only within the overlay bounds.
        var outset = DpToPixels(DragOutsetDp);
        var drag = new RectF(
            _imageRectOnCanvas.Left - outset,
            _imageRectOnCanvas.Top - outset,
            _imageRectOnCanvas.Width + 2 * outset,
            _imageRectOnCanvas.Height + 2 * outset);

        // Visible bounds = the overlay's full content area (0..Width/Height).
        // NOTE: do NOT subtract any extra gutter here; ImageHost.Padding already
        // moved the overlay's (0,0) away from the card edges.
        var visible = new RectF(
            0f,
            0f,
            (float)Overlay.Width,
            (float)Overlay.Height);

        _dragClampRect = IntersectRects(drag, visible);

        _drawable.ImageRect = _imageRectOnCanvas;
        _drawable.HandleRadiusPx = DpToPixels(HandleRadiusDp);

        Overlay.Invalidate();
    }

    private static RectF IntersectRects(RectF a, RectF b)
    {
        var left = Math.Max(a.Left, b.Left);
        var top = Math.Max(a.Top, b.Top);
        var right = Math.Min(a.Right, b.Right);
        var bottom = Math.Min(a.Bottom, b.Bottom);
        if (right < left || bottom < top) return new RectF(0, 0, 0, 0);
        return new RectF(left, top, right - left, bottom - top);
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

    private float DpToPixels(float dp) => (float)(dp * DeviceDisplay.MainDisplayInfo.Density);

    // --- touch ---
    private void OverlayOnStartInteraction(object? sender, TouchEventArgs e)
    {
        if (!e.Touches.Any()) return;
        var p = e.Touches.First();
        _draggingIndex = _drawable.HitTestHandle(new PointF((float)p.X, (float)p.Y), DpToPixels(HitRadiusDp));
    }

    private void OverlayOnDragInteraction(object? sender, TouchEventArgs e)
    {
        if (_draggingIndex is null || !e.Touches.Any()) return;

        var p = e.Touches.First();
        var newPt = new PointF((float)p.X, (float)p.Y);

        // Clamp to the enlarged working rect (can go outside image)
        var clamped = new PointF(
            Math.Clamp(newPt.X, _dragClampRect.Left, _dragClampRect.Right),
            Math.Clamp(newPt.Y, _dragClampRect.Top, _dragClampRect.Bottom));

        _drawable.MoveHandle(_draggingIndex.Value, clamped);
        Overlay.Invalidate();
    }

    private void OverlayOnEndInteraction(object? sender, TouchEventArgs e) => _draggingIndex = null;

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        _imagePage.FrameCrop = _drawable.GetNormalizedCorners(); // no popup
        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e) => await Navigation.PopAsync();

#if ANDROID
    private RectF? TryGetAndroidDisplayedImageRectInDips()
    {
        var handler = EditableImage?.Handler;
        if (handler == null) return null;

        var imageView = handler.PlatformView as global::Android.Widget.ImageView;
        var drawable  = imageView?.Drawable;
        if (imageView == null || drawable == null) return null;

        var mapped = new global::Android.Graphics.RectF(0, 0, drawable.IntrinsicWidth, drawable.IntrinsicHeight);
        var matrix = new global::Android.Graphics.Matrix(imageView.ImageMatrix);
        matrix.MapRect(mapped);
        mapped.Offset(imageView.PaddingLeft, imageView.PaddingTop);

        var density = (float)DeviceDisplay.MainDisplayInfo.Density;
        return new RectF(mapped.Left / density, mapped.Top / density, mapped.Width() / density, mapped.Height() / density);
    }
#endif

    // === overlay drawable ===
    private sealed class FrameDrawable : IDrawable
    {
        private readonly PointF[] _canvasPoints = new PointF[4];
        private CropQuadNormalized _normalized = CropQuadNormalized.FullImage;

        public RectF ImageRect { get; set; }
        public float HandleRadiusPx { get; set; } = 12f;

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
                canvas.FillCircle(p, HandleRadiusPx);
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
                if (dx * dx + dy * dy <= hitRadiusPx * hitRadiusPx)
                    return i;
            }
            return null;
        }

        public void MoveHandle(int index, PointF newCanvasPoint)
        {
            _canvasPoints[index] = newCanvasPoint;
            // DO NOT clamp to [0,1] — lets user place handles slightly outside the image
            var n = CanvasToNormalized(newCanvasPoint);
            _normalized = index switch
            {
                0 => _normalized with { TL = n },
                1 => _normalized with { TR = n },
                2 => _normalized with { BR = n },
                3 => _normalized with { BL = n },
                _ => _normalized
            };
        }

        private PointF NormalizedToCanvas(PointD n) =>
            new((float)(ImageRect.Left + n.X * ImageRect.Width),
                (float)(ImageRect.Top + n.Y * ImageRect.Height));

        private PointD CanvasToNormalized(PointF c)
        {
            if (ImageRect.Width <= 0 || ImageRect.Height <= 0) return new PointD(0, 0);
            var x = (c.X - ImageRect.Left) / ImageRect.Width;
            var y = (c.Y - ImageRect.Top) / ImageRect.Height;
            return new PointD(x, y); // not clamped
        }

        private PointD GetNorm(int i) => i switch
        {
            0 => _normalized.TL,
            1 => _normalized.TR,
            2 => _normalized.BR,
            3 => _normalized.BL,
            _ => _normalized.TL
        };
    }
}
