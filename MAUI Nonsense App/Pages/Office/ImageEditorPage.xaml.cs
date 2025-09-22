using System;
using System.IO;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Devices;
using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages.Office;

public partial class ImageEditorPage : ContentPage
{
    private readonly ImagePageModel _imagePage;

    private readonly FrameDrawable _drawable;
    private RectF _imageRectOnCanvas;      // where the bitmap actually draws inside the Overlay
    private RectF _dragClampRect;          // a slightly bigger rect = imageRect inflated for “outside” dragging
    private Size _bitmapSize;
    private int? _draggingIndex = null;

    // UI affordances (in dp)
    private const float HandleRadiusDp = 10f;
    private const float HitRadiusDp = 28f;
    private const float DragOutsetDp = 24f;   // how far outside the image you can drag

    public ImageEditorPage(ImagePageModel imagePage)
    {
        InitializeComponent();
        _imagePage = imagePage;

        _drawable = new FrameDrawable();
        Overlay.Drawable = _drawable;

        // touch
        Overlay.StartInteraction += OverlayOnStartInteraction;
        Overlay.DragInteraction += OverlayOnDragInteraction;
        Overlay.EndInteraction += OverlayOnEndInteraction;

        // layout changes
        Overlay.SizeChanged += (_, __) => RecomputeImageRectAndRedraw();
        EditableImage.SizeChanged += (_, __) => RecomputeImageRectAndRedraw();

        LoadImage();
    }

    private void LoadImage()
    {
        if (!File.Exists(_imagePage.FilePath)) return;

        EditableImage.Source = ImageSource.FromFile(_imagePage.FilePath);

        // If selection page filled these, use them; otherwise detect once.
        if (_imagePage.OriginalPixelWidth <= 0 || _imagePage.OriginalPixelHeight <= 0)
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
            catch { _bitmapSize = new Size(1000, 1000); }
        }
        else
        {
            _bitmapSize = new Size(_imagePage.OriginalPixelWidth, _imagePage.OriginalPixelHeight);
        }

        // Seed corners (exact full-image) if nothing saved yet
        _drawable.SetNormalizedCorners(_imagePage.FrameCrop ?? CropQuadNormalized.FullImage);

        RecomputeImageRectAndRedraw();
    }

    private void RecomputeImageRectAndRedraw()
    {
        // Get the exact rect where the platform drew the bitmap (best), else math fallback
#if ANDROID
        var rectFromAndroid = TryGetAndroidDisplayedImageRectInDips();
        if (rectFromAndroid.HasValue)
            _imageRectOnCanvas = rectFromAndroid.Value;
        else
#endif
        {
            if (Overlay.Width <= 0 || Overlay.Height <= 0 || _bitmapSize.Width <= 0 || _bitmapSize.Height <= 0)
                return;

            _imageRectOnCanvas = ComputeAspectFitRect(new Size(Overlay.Width, Overlay.Height), _bitmapSize);
        }

        // allow dragging slightly outside image
        var outset = DpToPixels(DragOutsetDp);
        _dragClampRect = _imageRectOnCanvas;
        _dragClampRect.X -= outset;
        _dragClampRect.Y -= outset;
        _dragClampRect.Width += 2 * outset;
        _dragClampRect.Height += 2 * outset;

        _drawable.ImageRect = _imageRectOnCanvas;
        _drawable.HandleRadiusPx = DpToPixels(HandleRadiusDp);

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

    private float DpToPixels(float dp) => (float)(dp * DeviceDisplay.MainDisplayInfo.Density);

    // -------- touch handlers --------
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

        // Clamp to our enlarged rect so you can go a bit outside the photo
        var clamped = new PointF(
            Math.Clamp(newPt.X, _dragClampRect.Left, _dragClampRect.Right),
            Math.Clamp(newPt.Y, _dragClampRect.Top, _dragClampRect.Bottom)
        );

        _drawable.MoveHandle(_draggingIndex.Value, clamped);
        Overlay.Invalidate();
    }

    private void OverlayOnEndInteraction(object? sender, TouchEventArgs e)
    {
        _draggingIndex = null;
    }

    // -------- actions --------
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Save without any popups
        _imagePage.FrameCrop = _drawable.GetNormalizedCorners();
        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

#if ANDROID
    /// <summary>
    /// Exact rectangle where Android's ImageView drew the bitmap (in DIPs).
    /// </summary>
    private RectF? TryGetAndroidDisplayedImageRectInDips()
    {
        var handler = EditableImage?.Handler;
        if (handler == null) return null;

        var imageView = handler.PlatformView as global::Android.Widget.ImageView;
        var drawable  = imageView?.Drawable;
        if (imageView == null || drawable == null) return null;

        // drawable source rect (px)
        var mapped = new global::Android.Graphics.RectF(0, 0, drawable.IntrinsicWidth, drawable.IntrinsicHeight);

        // apply the matrix used by ImageView (px space)
        var matrix = new global::Android.Graphics.Matrix(imageView.ImageMatrix);
        matrix.MapRect(mapped);

        // include view padding (px)
        mapped.Offset(imageView.PaddingLeft, imageView.PaddingTop);

        var density = (float)DeviceDisplay.MainDisplayInfo.Density;
        return new RectF(mapped.Left / density, mapped.Top / density, mapped.Width() / density, mapped.Height() / density);
    }
#endif

    // ================= draw overlay =================
    private sealed class FrameDrawable : IDrawable
    {
        // order: TL(0) TR(1) BR(2) BL(3)
        private readonly PointF[] _canvasPoints = new PointF[4];
        private CropQuadNormalized _normalized = CropQuadNormalized.FullImage;

        public RectF ImageRect { get; set; }
        public float HandleRadiusPx { get; set; } = 10f;

        private const float LineThickness = 2f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // map normalized to canvas points each frame
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
                if ((dx * dx + dy * dy) <= hitRadiusPx * hitRadiusPx)
                    return i;
            }
            return null;
        }

        public void MoveHandle(int index, PointF newCanvasPoint)
        {
            _canvasPoints[index] = newCanvasPoint;

            // convert to normalized relative to *imageRect*, but do not clamp:
            // allow values <0 or >1 so user can place handles outside.
            var n = CanvasToNormalized(newCanvasPoint);
            SetNorm(index, n);
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
            // NOTE: intentionally NOT clamped to [0,1]
            return new PointD(x, y);
        }

        private PointD GetNorm(int i) => i switch
        {
            0 => _normalized.TL,
            1 => _normalized.TR,
            2 => _normalized.BR,
            3 => _normalized.BL,
            _ => _normalized.TL
        };

        private void SetNorm(int i, PointD v)
        {
            _normalized = i switch
            {
                0 => _normalized with { TL = v },
                1 => _normalized with { TR = v },
                2 => _normalized with { BR = v },
                3 => _normalized with { BL = v },
                _ => _normalized
            };
        }
    }
}
