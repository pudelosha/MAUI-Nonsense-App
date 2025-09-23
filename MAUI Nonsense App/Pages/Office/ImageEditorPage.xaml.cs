using System;
using System.IO;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using MAUI_Nonsense_App.Models;

// avoid conflict with Controls.Image
using GImage = Microsoft.Maui.Graphics.IImage;

namespace MAUI_Nonsense_App.Pages.Office;

public partial class ImageEditorPage : ContentPage
{
    private readonly ImagePageModel _imagePage;

    private readonly QuadOverlayDrawable _drawable;
    private RectF _imageRectOnOverlay;   // bitmap rect in OVERLAY coordinates
    private RectF _dragClampRect;        // where we allow dragging (overlay area, expanded)
    private Size _bitmapSize;           // raw pixel W/H from file (pre-EXIF)
    private Size _displaySize;          // size as DISPLAYED (post-EXIF rotation: may be swapped)

    // EXIF orientation mapping (Android)
    private ExifOrientation _exifOrientation = ExifOrientation.Normal;

    // UI knobs (DIPs)
    private const float HandleRadiusDp = 14f;  // green circle radius
    private const float HitRadiusDp = 28f;  // touch halo
    private const float DragOutsetDp = 32f;  // how far outside image you can drag

    public ImageEditorPage(ImagePageModel imagePage)
    {
        InitializeComponent();
        _imagePage = imagePage;

        _drawable = new QuadOverlayDrawable();
        Overlay.Drawable = _drawable;

        // interactions
        Overlay.StartInteraction += OverlayOnStartInteraction;
        Overlay.DragInteraction += OverlayOnDragInteraction;
        Overlay.EndInteraction += OverlayOnEndInteraction;

        // recompute rects when layout changes
        Overlay.SizeChanged += (_, __) => RecomputeRectsAndRedraw();
        ImageArea.SizeChanged += (_, __) => RecomputeRectsAndRedraw();
        EditableImage.SizeChanged += (_, __) => RecomputeRectsAndRedraw();
        ImageArea.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(VisualElement.X) || e.PropertyName == nameof(VisualElement.Y))
                RecomputeRectsAndRedraw();
        };

        LoadImage();
    }

    private void LoadImage()
    {
        if (!File.Exists(_imagePage.FilePath))
            return;

        EditableImage.Source = ImageSource.FromFile(_imagePage.FilePath);

#if ANDROID
        _exifOrientation = ReadAndroidExifOrientation(_imagePage.FilePath);
#else
        _exifOrientation = ExifOrientation.Normal;
#endif

        try
        {
            using var fs = File.OpenRead(_imagePage.FilePath);
            var gimg = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(fs);
            _drawable.SetImage(gimg);
            _bitmapSize = new Size(gimg.Width, gimg.Height);
        }
        catch
        {
            _bitmapSize = new Size(
                _imagePage.OriginalPixelWidth > 0 ? _imagePage.OriginalPixelWidth : 1000,
                _imagePage.OriginalPixelHeight > 0 ? _imagePage.OriginalPixelHeight : 1000);
        }

        // displayed size = raw size rotated by EXIF (90/270 swaps)
        _displaySize = NeedsSwap(_exifOrientation)
            ? new Size(_bitmapSize.Height, _bitmapSize.Width)
            : _bitmapSize;

        _drawable.SetNormalizedCorners(_imagePage.FrameCrop ?? CropQuadNormalized.FullImage);
        _drawable.SetOrientation(_exifOrientation); // for loupe sampling
        RecomputeRectsAndRedraw();
    }

    private void RecomputeRectsAndRedraw()
    {
        if (Overlay.Width <= 0 || Overlay.Height <= 0 ||
            ImageArea.Width <= 0 || ImageArea.Height <= 0 ||
            _displaySize.Width <= 0 || _displaySize.Height <= 0)
            return;

        // 1) Image rect inside ImageArea (AspectFit USING DISPLAY SIZE)
        var rectInImageArea = ComputeAspectFitRect(new Size(ImageArea.Width, ImageArea.Height), _displaySize);

        // 2) Translate to OVERLAY space
        var left = (float)(ImageArea.X + rectInImageArea.Left);
        var top = (float)(ImageArea.Y + rectInImageArea.Top);
        _imageRectOnOverlay = new RectF(left, top, rectInImageArea.Width, rectInImageArea.Height);

        // 3) Drag clamp region = expanded image rect ∩ overlay bounds
        var outset = DpToPixels(DragOutsetDp);
        var expanded = new RectF(
            _imageRectOnOverlay.Left - outset,
            _imageRectOnOverlay.Top - outset,
            _imageRectOnOverlay.Width + 2 * outset,
            _imageRectOnOverlay.Height + 2 * outset);

        var overlayBounds = new RectF(0, 0, (float)Overlay.Width, (float)Overlay.Height);
        _dragClampRect = IntersectRects(expanded, overlayBounds);

        // wire drawable
        _drawable.ImageRect = _imageRectOnOverlay;
        _drawable.OverlayBounds = overlayBounds;
        _drawable.HandleRadiusPx = DpToPixels(HandleRadiusDp);

        // keep loupe radius sane vs overlay size
        var maxR = (float)(Math.Min(Overlay.Width, Overlay.Height) * 0.45);
        if (_drawable.MagnifierRadiusPx > maxR)
            _drawable.MagnifierRadiusPx = maxR;

        _drawable.UpdateMagnifierCenter();
        Overlay.Invalidate();
    }

    // ---------- interaction ----------
    private int? _draggingIndex;

    private void OverlayOnStartInteraction(object? sender, TouchEventArgs e)
    {
        if (!e.Touches.Any()) return;

        var p = e.Touches.First();
        var pt = new PointF((float)p.X, (float)p.Y);

        _draggingIndex = _drawable.HitTestHandle(pt, DpToPixels(HitRadiusDp));

        // show loupe
        _drawable.MagnifierVisible = true;
        _drawable.MagnifierFocusCanvas = pt;
        PickMagnifierOffset(pt);
        _drawable.UpdateMagnifierCenter();

        Overlay.Invalidate();
    }

    private void OverlayOnDragInteraction(object? sender, TouchEventArgs e)
    {
        if (!e.Touches.Any()) return;

        var p = e.Touches.First();
        var pt = new PointF((float)p.X, (float)p.Y);

        if (_draggingIndex is not null)
        {
            var clamped = new PointF(
                Math.Clamp(pt.X, _dragClampRect.Left, _dragClampRect.Right),
                Math.Clamp(pt.Y, _dragClampRect.Top, _dragClampRect.Bottom));
            _drawable.MoveHandle(_draggingIndex.Value, clamped);
        }

        _drawable.MagnifierVisible = true;
        _drawable.MagnifierFocusCanvas = pt;

        if (!IsLensComfortablyOnScreen(pt, _drawable.MagnifierCenterCanvas, _drawable.MagnifierRadiusPx))
            PickMagnifierOffset(pt);

        _drawable.UpdateMagnifierCenter();
        Overlay.Invalidate();
    }

    private void OverlayOnEndInteraction(object? sender, TouchEventArgs e)
    {
        _draggingIndex = null;
        _drawable.MagnifierVisible = false;
        Overlay.Invalidate();
    }

    private bool IsLensComfortablyOnScreen(PointF focus, PointF center, float radius)
    {
        const float margin = 6f;
        var left = center.X - radius - margin;
        var right = center.X + radius + margin;
        var top = center.Y - radius - margin;
        var bottom = center.Y + radius + margin;
        return left >= 0 && top >= 0 &&
               right <= _drawable.OverlayBounds.Right &&
               bottom <= _drawable.OverlayBounds.Bottom;
    }

    private void PickMagnifierOffset(PointF focus)
    {
        var r = _drawable.MagnifierRadiusPx;
        float pad = 16f;
        bool preferRight = focus.X < _drawable.OverlayBounds.Width * 0.5f;
        bool preferDown = focus.Y < _drawable.OverlayBounds.Height * 0.5f;
        _drawable.MagnifierOffsetPx = new PointF(
            preferRight ? (r + pad) : -(r + pad),
            preferDown ? (r + pad) : -(r + pad));
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Save in DISPLAY coordinates (what user sees)
        _imagePage.FrameCrop = _drawable.GetNormalizedCorners();
        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    // ---------- helpers ----------
    private static RectF IntersectRects(RectF a, RectF b)
    {
        var l = Math.Max(a.Left, b.Left);
        var t = Math.Max(a.Top, b.Top);
        var r = Math.Min(a.Right, b.Right);
        var btm = Math.Min(a.Bottom, b.Bottom);
        if (r < l || btm < t) return new RectF(0, 0, 0, 0);
        return new RectF(l, t, r - l, btm - t);
    }

    private static RectF ComputeAspectFitRect(Size container, Size image)
    {
        if (container.Width <= 0 || container.Height <= 0 || image.Width <= 0 || image.Height <= 0)
            return new RectF(0, 0, 0, 0);

        var scale = (float)Math.Min(container.Width / image.Width, container.Height / image.Height);
        var w = (float)(image.Width * scale);
        var h = (float)(image.Height * scale);
        var x = (float)((container.Width - w) / 2.0);
        var y = (float)((container.Height - h) / 2.0);
        return new RectF(x, y, w, h);
    }

    private float DpToPixels(float dp) => (float)(dp * DeviceDisplay.MainDisplayInfo.Density);

    private static bool NeedsSwap(ExifOrientation o) =>
        o == ExifOrientation.Rotate90 || o == ExifOrientation.Rotate270 ||
        o == ExifOrientation.Transpose || o == ExifOrientation.Transverse;

#if ANDROID
    private static ExifOrientation ReadAndroidExifOrientation(string path)
    {
        try
        {
            var exif = new AndroidX.ExifInterface.Media.ExifInterface(path);
            var val = exif.GetAttributeInt(AndroidX.ExifInterface.Media.ExifInterface.TagOrientation,
                                           (int)AndroidX.ExifInterface.Media.ExifInterface.OrientationNormal);
            return val switch
            {
                (int)AndroidX.ExifInterface.Media.ExifInterface.OrientationRotate90   => ExifOrientation.Rotate90,
                (int)AndroidX.ExifInterface.Media.ExifInterface.OrientationRotate180  => ExifOrientation.Rotate180,
                (int)AndroidX.ExifInterface.Media.ExifInterface.OrientationRotate270  => ExifOrientation.Rotate270,
                (int)AndroidX.ExifInterface.Media.ExifInterface.OrientationFlipHorizontal => ExifOrientation.FlipH,
                (int)AndroidX.ExifInterface.Media.ExifInterface.OrientationFlipVertical   => ExifOrientation.FlipV,
                (int)AndroidX.ExifInterface.Media.ExifInterface.OrientationTranspose      => ExifOrientation.Transpose,
                (int)AndroidX.ExifInterface.Media.ExifInterface.OrientationTransverse     => ExifOrientation.Transverse,
                _ => ExifOrientation.Normal,
            };
        }
        catch { return ExifOrientation.Normal; }
    }
#endif

    // ===================== Overlay drawable (handles + loupe) =====================
    private sealed class QuadOverlayDrawable : IDrawable
    {
        public RectF ImageRect { get; set; }
        public RectF OverlayBounds { get; set; }

        // handles
        public float HandleRadiusPx { get; set; } = 14f;
        private readonly PointF[] _canvasPoints = new PointF[4];
        private CropQuadNormalized _normalized = CropQuadNormalized.FullImage;

        // loupe
        public float MagnifierRadiusPx { get; set; } = 56f;
        public bool MagnifierVisible { get; set; }
        public PointF MagnifierFocusCanvas { get; set; }   // overlay coords
        public PointF MagnifierCenterCanvas { get; set; }
        public PointF MagnifierOffsetPx { get; set; }

        // source image and EXIF orientation
        private GImage? _image;
        private ExifOrientation _exif = ExifOrientation.Normal;

        public void SetImage(GImage img) => _image = img;
        public void SetOrientation(ExifOrientation exif) => _exif = exif;

        public void SetNormalizedCorners(CropQuadNormalized quad) => _normalized = quad;
        public CropQuadNormalized GetNormalizedCorners() => _normalized;

        public void UpdateMagnifierCenter()
        {
            var cx = MagnifierFocusCanvas.X + MagnifierOffsetPx.X;
            var cy = MagnifierFocusCanvas.Y + MagnifierOffsetPx.Y;
            var r = MagnifierRadiusPx;

            float left = OverlayBounds.Left + r + 2;
            float right = OverlayBounds.Right - r - 2;
            float top = OverlayBounds.Top + r + 2;
            float bottom = OverlayBounds.Bottom - r - 2;

            if (right <= left || bottom <= top)
            {
                MagnifierCenterCanvas = new PointF(
                    OverlayBounds.Left + OverlayBounds.Width * 0.5f,
                    OverlayBounds.Top + OverlayBounds.Height * 0.5f);
                return;
            }

            MagnifierCenterCanvas = new PointF(
                Math.Clamp(cx, left, right),
                Math.Clamp(cy, top, bottom));
        }

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
            var n = CanvasToNormalized(newCanvasPoint); // display-normalized (can be <0 or >1)
            _normalized = index switch
            {
                0 => _normalized with { TL = n },
                1 => _normalized with { TR = n },
                2 => _normalized with { BR = n },
                3 => _normalized with { BL = n },
                _ => _normalized
            };
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // Map normalized crop to overlay pixels
            for (int i = 0; i < 4; i++)
                _canvasPoints[i] = NormalizedToCanvas(GetNorm(i));

            // lines
            canvas.StrokeColor = Colors.LimeGreen;
            canvas.StrokeSize = 2f;
            for (int i = 0; i < 4; i++)
            {
                var a = _canvasPoints[i];
                var b = _canvasPoints[(i + 1) % 4];
                canvas.DrawLine(a, b);
            }

            // circular handles
            canvas.FillColor = Colors.LimeGreen;
            foreach (var p in _canvasPoints)
                canvas.FillCircle(p, HandleRadiusPx);

            // loupe
            if (MagnifierVisible && _image != null && ImageRect.Width > 0 && ImageRect.Height > 0)
                DrawMagnifier(canvas);
        }

        private void DrawMagnifier(ICanvas canvas)
        {
            if (_image is null) return;

            float r = MagnifierRadiusPx;
            float d = 2 * r;

            var destLeft = MagnifierCenterCanvas.X - r;
            var destTop = MagnifierCenterCanvas.Y - r;

            // DISPLAY-normalized focus relative to drawn image rect
            // IMPORTANT: NO CLAMP — we want true outside behavior
            var xDisp = (MagnifierFocusCanvas.X - ImageRect.Left) / ImageRect.Width;
            var yDisp = (MagnifierFocusCanvas.Y - ImageRect.Top) / ImageRect.Height;

            // Convert display-normalized to SOURCE-normalized using EXIF orientation
            var (xSrc, ySrc) = DisplayToSource(xDisp, yDisp, _exif);

            var imgW = (float)_image.Width;
            var imgH = (float)_image.Height;

            // focus in source pixels (can be outside 0..imgW/H)
            var fxPx = xSrc * imgW;
            var fyPx = ySrc * imgH;

            // pixels-per-DIP of the displayed image
            var pxPerDipX = imgW / ImageRect.Width;
            var pxPerDipY = imgH / ImageRect.Height;

            // Zoom=2 → 2x magnification
            const float Zoom = 2.0f;
            var sampleWpx = Math.Max(1f, (d / Zoom) * pxPerDipX);
            var sampleHpx = Math.Max(1f, (d / Zoom) * pxPerDipY);

            // centered source rect — DO NOT CLAMP; parts outside image will be empty
            float srcLeftPx = fxPx - sampleWpx / 2f;
            float srcTopPx = fyPx - sampleHpx / 2f;

            // circular clip
            var clip = new PathF();
            clip.AppendCircle(MagnifierCenterCanvas, r);

            canvas.SaveState();
            canvas.ClipPath(clip);

            // scale so source area maps to lens size
            var scaleX = d / sampleWpx;
            var scaleY = d / sampleHpx;

            // position so that (srcLeftPx,srcTopPx) lands at (destLeft,destTop)
            canvas.Translate(destLeft - srcLeftPx * scaleX, destTop - srcTopPx * scaleY);
            canvas.Scale(scaleX, scaleY);

            // draw the entire image at pixel size; areas outside remain empty → show gray
            canvas.DrawImage(_image, 0, 0, imgW, imgH);

            canvas.RestoreState();

            // crosshair + ring
            var crossShadow = Colors.Black.WithAlpha(0.35f);
            var crossStroke = Colors.White.WithAlpha(0.9f);

            canvas.StrokeSize = 3f; canvas.StrokeColor = crossShadow;
            canvas.DrawLine(MagnifierCenterCanvas.X - (r - 6), MagnifierCenterCanvas.Y,
                            MagnifierCenterCanvas.X + (r - 6), MagnifierCenterCanvas.Y);
            canvas.DrawLine(MagnifierCenterCanvas.X, MagnifierCenterCanvas.Y - (r - 6),
                            MagnifierCenterCanvas.X, MagnifierCenterCanvas.Y + (r - 6));

            canvas.StrokeSize = 1.5f; canvas.StrokeColor = crossStroke;
            canvas.DrawLine(MagnifierCenterCanvas.X - (r - 6), MagnifierCenterCanvas.Y,
                            MagnifierCenterCanvas.X + (r - 6), MagnifierCenterCanvas.Y);
            canvas.DrawLine(MagnifierCenterCanvas.X, MagnifierCenterCanvas.Y - (r - 6),
                            MagnifierCenterCanvas.X, MagnifierCenterCanvas.Y + (r - 6));

            canvas.FillColor = Colors.Black.WithAlpha(0.25f);
            canvas.FillCircle(MagnifierCenterCanvas, 2.5f);

            canvas.StrokeColor = Colors.Black.WithAlpha(0.20f);
            canvas.StrokeSize = 3f;
            canvas.DrawCircle(MagnifierCenterCanvas, r);
        }

        // ---- mapping helpers ----

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

        // DISPLAY-normalized → SOURCE-normalized using EXIF orientation
        private static (float x, float y) DisplayToSource(double x, double y, ExifOrientation exif)
        {
            float xf = (float)x, yf = (float)y;

            return exif switch
            {
                ExifOrientation.Rotate90 => (yf, 1f - xf),
                ExifOrientation.Rotate180 => (1f - xf, 1f - yf),
                ExifOrientation.Rotate270 => (1f - yf, xf),

                ExifOrientation.FlipH => (1f - xf, yf),
                ExifOrientation.FlipV => (xf, 1f - yf),

                ExifOrientation.Transpose => (yf, xf),
                ExifOrientation.Transverse => (1f - yf, 1f - xf),

                _ => (xf, yf)
            };
        }
    }

    // Minimal EXIF orientation enum we use
    private enum ExifOrientation
    {
        Normal = 1,
        Rotate90 = 6,
        Rotate180 = 3,
        Rotate270 = 8,
        FlipH = 2,
        FlipV = 4,
        Transpose = 7,
        Transverse = 5
    }
}
