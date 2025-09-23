using AndroidX.ExifInterface.Media;
using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;
using SkiaSharp;

namespace MAUI_Nonsense_App.Platforms.Android.Services.Office;

public class DocumentBuilderService : IDocumentBuilderService
{
    // ---------------- IDocumentBuilderService ----------------

    public async Task<string?> CapturePhotoAsync()
    {
        try
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            return photo is not null ? await SaveTempImageAsync(photo) : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CapturePhotoAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<List<string>> PickImagesAsync()
    {
        var results = new List<string>();
        try
        {
            var photos = await FilePicker.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Select images",
                FileTypes = FilePickerFileType.Images
            });

            foreach (var photo in photos)
                results.Add(await SaveTempImageAsync(photo));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PickImagesAsync error: {ex.Message}");
        }
        return results;
    }

    public async Task<string> SaveTempImageAsync(FileResult file)
    {
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var dest = Path.Combine(FileSystem.CacheDirectory, fileName);

        using var stream = await file.OpenReadAsync();
        using var newStream = File.OpenWrite(dest);
        await stream.CopyToAsync(newStream);

        return dest;
    }

    // Legacy helper – left for compatibility; now uses the real writer with default quality 85
    public Task<bool> GeneratePdfAsync(PdfCreationSession session, string outputPath, string? password)
        => CreatePdfInternalAsync(outputPath, session.Pages, jpegQuality: 85);

    // NEW signature (with quality from your slider)
    public async Task<bool> CreatePdfAsync(string name, string? password, List<ImagePageModel> pages, int jpegQuality)
    {
        try
        {
            if (pages == null || pages.Count == 0) return false;

            var safeName = MakeSafeFileName(string.IsNullOrWhiteSpace(name)
                ? $"Document_{DateTime.Now:yyyyMMdd_HHmmss}"
                : name.Trim());

            var outputDir = FileSystem.AppDataDirectory;
            Directory.CreateDirectory(outputDir);

            var path = GetUniquePath(Path.Combine(outputDir, $"{safeName}.pdf"));
            var ok = await CreatePdfInternalAsync(path, pages, jpegQuality);
            return ok;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CreatePdfAsync error: {ex.Message}");
            return false;
        }
    }

    // ---------------- internals ----------------

    private static string MakeSafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static string GetUniquePath(string basePath)
    {
        if (!File.Exists(basePath)) return basePath;

        var dir = Path.GetDirectoryName(basePath)!;
        var file = Path.GetFileNameWithoutExtension(basePath);
        var ext = Path.GetExtension(basePath);
        int i = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{file} ({i++}){ext}");
        } while (File.Exists(candidate));
        return candidate;
    }

    /// <summary>
    /// Core PDF writer.
    /// - If the crop quad is close to a rectangle → fast axis-aligned crop.
    /// - Else → perspective warp via homography (with pre-downscale for speed).
    /// Encodes pages as JPEG with the requested quality.
    /// </summary>
    private static async Task<bool> CreatePdfInternalAsync(string outputPath, List<ImagePageModel> pages, int jpegQuality)
    {
        try
        {
            int q = Math.Clamp(jpegQuality, 1, 100);

            using var fs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var doc = SKDocument.CreatePdf(fs);

            foreach (var p in pages)
            {
                if (!File.Exists(p.FilePath)) continue;

                using var upright = LoadBitmapUpright(p.FilePath);
                if (upright is null) continue;

                // Read quad (may be null)
                var quadNorm = p.FrameCrop ?? CropQuadNormalized.FullImage;

                // Convert normalized quad → source pixel quad (allow outside range)
                var srcQ = new SKPoint[4];
                srcQ[0] = new SKPoint((float)(quadNorm.TL.X * upright.Width), (float)(quadNorm.TL.Y * upright.Height));
                srcQ[1] = new SKPoint((float)(quadNorm.TR.X * upright.Width), (float)(quadNorm.TR.Y * upright.Height));
                srcQ[2] = new SKPoint((float)(quadNorm.BR.X * upright.Width), (float)(quadNorm.BR.Y * upright.Height));
                srcQ[3] = new SKPoint((float)(quadNorm.BL.X * upright.Width), (float)(quadNorm.BL.Y * upright.Height));

                // Clamp quad to bitmap bounds for safety when making a rect.
                var rectCrop = ToCropRect(upright.Width, upright.Height, quadNorm);
                rectCrop = SKRectI.Intersect(rectCrop, new SKRectI(0, 0, upright.Width, upright.Height));

                // Heuristic: if the quad is “close to rectangular”, do the fast path
                if (!IsQuadPerspective(srcQ))
                {
                    if (rectCrop.IsEmpty) continue;

                    using var cropped = new SKBitmap(rectCrop.Width, rectCrop.Height, upright.ColorType, upright.AlphaType);
                    using (var canvas = new SKCanvas(cropped))
                    {
                        var src = new SKRect(rectCrop.Left, rectCrop.Top, rectCrop.Right, rectCrop.Bottom);
                        var dst = new SKRect(0, 0, rectCrop.Width, rectCrop.Height);
                        canvas.Clear(SKColors.White);
                        canvas.DrawBitmap(upright, src, dst);
                    }

                    using var img = SKImage.FromBitmap(cropped);
                    using var encoded = img.Encode(SKEncodedImageFormat.Jpeg, q);

                    using var page = doc.BeginPage(cropped.Width, cropped.Height);
                    using var encImg = SKImage.FromEncodedData(encoded);
                    page.DrawImage(encImg, new SKRect(0, 0, cropped.Width, cropped.Height));
                    doc.EndPage();
                }
                else
                {
                    // PERSPECTIVE path — pre-downscale for speed
                    // Output size approx from opposite-edge lengths
                    int outW = (int)Math.Round(Math.Max(Distance(srcQ[0], srcQ[1]), Distance(srcQ[3], srcQ[2])));
                    int outH = (int)Math.Round(Math.Max(Distance(srcQ[0], srcQ[3]), Distance(srcQ[1], srcQ[2])));
                    outW = Math.Max(1, outW);
                    outH = Math.Max(1, outH);

                    // Downscale source to limit warp cost, and scale quad accordingly
                    const int MAX_WARP_DIM = 2000; // tweak 1600–2400 for speed/quality tradeoff
                    using var srcForWarp = DownscaleForWarp(upright, ref srcQ, MAX_WARP_DIM);

                    // Recompute suggested output size from the scaled quad (keeps aspect consistent)
                    outW = Math.Max(1, (int)Math.Round(Math.Max(Distance(srcQ[0], srcQ[1]), Distance(srcQ[3], srcQ[2]))));
                    outH = Math.Max(1, (int)Math.Round(Math.Max(Distance(srcQ[0], srcQ[3]), Distance(srcQ[1], srcQ[2]))));

                    using var warped = WarpPerspectiveSafe(srcForWarp, srcQ, outW, outH, SKColors.White);

                    using var img = SKImage.FromBitmap(warped);
                    using var encoded = img.Encode(SKEncodedImageFormat.Jpeg, q);

                    using var page = doc.BeginPage(warped.Width, warped.Height);
                    using var encImg = SKImage.FromEncodedData(encoded);
                    page.DrawImage(encImg, new SKRect(0, 0, warped.Width, warped.Height));
                    doc.EndPage();
                }
            }

            doc.Close();
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF write error: {ex.Message}");
            try { if (File.Exists(outputPath)) File.Delete(outputPath); } catch { }
            return false;
        }
    }

    // ---------------- image helpers ----------------

    /// <summary>Load bitmap and rotate/flip to upright based on EXIF (Android).</summary>
    private static SKBitmap? LoadBitmapUpright(string path)
    {
        try
        {
            using var data = SKData.Create(path);
            using var codec = SKCodec.Create(data);
            if (codec == null) return SKBitmap.Decode(path);

            var info = codec.Info;
            var bmp = new SKBitmap(info.Width, info.Height, info.ColorType, info.AlphaType);
            codec.GetPixels(bmp.Info, bmp.GetPixels());

            int degrees = 0; bool flipH = false, flipV = false;
            try
            {
                var exif = new ExifInterface(path);
                var o = exif.GetAttributeInt(ExifInterface.TagOrientation, (int)ExifInterface.OrientationNormal);
                switch (o)
                {
                    case (int)ExifInterface.OrientationRotate90: degrees = 90; break;
                    case (int)ExifInterface.OrientationRotate180: degrees = 180; break;
                    case (int)ExifInterface.OrientationRotate270: degrees = 270; break;
                    case (int)ExifInterface.OrientationFlipHorizontal: flipH = true; break;
                    case (int)ExifInterface.OrientationFlipVertical: flipV = true; break;
                    case (int)ExifInterface.OrientationTranspose: degrees = 90; flipH = true; break;
                    case (int)ExifInterface.OrientationTransverse: degrees = 270; flipH = true; break;
                }
            }
            catch { /* ignore */ }

            if (degrees == 0 && !flipH && !flipV) return bmp;

            var dstInfo = new SKImageInfo(
                degrees is 90 or 270 ? bmp.Height : bmp.Width,
                degrees is 90 or 270 ? bmp.Width : bmp.Height,
                bmp.ColorType, bmp.AlphaType);

            using var surface = SKSurface.Create(dstInfo);
            var canvas = surface.Canvas;

            canvas.Translate(dstInfo.Width / 2f, dstInfo.Height / 2f);
            if (flipH || flipV) canvas.Scale(flipH ? -1 : 1, flipV ? -1 : 1);
            if (degrees != 0) canvas.RotateDegrees(degrees);
            canvas.Translate(-bmp.Width / 2f, -bmp.Height / 2f);
            canvas.DrawBitmap(bmp, 0, 0);
            canvas.Flush();

            bmp.Dispose();
            using var snapshot = surface.Snapshot();
            return SKBitmap.FromImage(snapshot);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Axis-aligned rect from normalized quad (values may be outside 0..1).</summary>
    private static SKRectI ToCropRect(int width, int height, CropQuadNormalized quad)
    {
        int x0 = (int)Math.Round(quad.TL.X * width);
        int y0 = (int)Math.Round(quad.TL.Y * height);
        int x1 = (int)Math.Round(quad.TR.X * width);
        int y1 = (int)Math.Round(quad.TR.Y * height);
        int x2 = (int)Math.Round(quad.BR.X * width);
        int y2 = (int)Math.Round(quad.BR.Y * height);
        int x3 = (int)Math.Round(quad.BL.X * width);
        int y3 = (int)Math.Round(quad.BL.Y * height);

        int left = Math.Min(Math.Min(x0, x1), Math.Min(x2, x3));
        int top = Math.Min(Math.Min(y0, y1), Math.Min(y2, y3));
        int right = Math.Max(Math.Max(x0, x1), Math.Max(x2, x3));
        int bottom = Math.Max(Math.Max(y0, y1), Math.Max(y2, y3));

        if (right <= left) right = left + 1;
        if (bottom <= top) bottom = top + 1;

        return new SKRectI(left, top, right, bottom);
    }

    private static float Distance(SKPoint a, SKPoint b)
        => (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    /// <summary>Heuristic: does the quad need perspective correction?</summary>
    private static bool IsQuadPerspective(SKPoint[] q)
    {
        // Compare opposite edge angles; if nearly parallel and rectangle-ish area, skip warp
        float angle01 = MathF.Atan2(q[1].Y - q[0].Y, q[1].X - q[0].X);
        float angle32 = MathF.Atan2(q[2].Y - q[3].Y, q[2].X - q[3].X);
        float angle03 = MathF.Atan2(q[3].Y - q[0].Y, q[3].X - q[0].X);
        float angle12 = MathF.Atan2(q[2].Y - q[1].Y, q[2].X - q[1].X);

        float d1 = Math.Abs(NormAngle(angle01 - angle32)); // top vs bottom
        float d2 = Math.Abs(NormAngle(angle03 - angle12)); // left vs right

        bool edgesParallel = d1 < 0.07f && d2 < 0.07f; // ~4 degrees
        if (edgesParallel)
        {
            // also check aspect ratio consistency as a tie-breaker
            var top = Distance(q[0], q[1]);
            var bottom = Distance(q[3], q[2]);
            var left = Distance(q[0], q[3]);
            var right = Distance(q[1], q[2]);

            var hRatio = top > 0 ? bottom / top : 1f;
            var vRatio = left > 0 ? right / left : 1f;

            if (Math.Abs(hRatio - 1f) < 0.12f && Math.Abs(vRatio - 1f) < 0.12f)
                return false;
        }
        return true;

        static float NormAngle(float a)
        {
            while (a > MathF.PI) a -= 2 * MathF.PI;
            while (a < -MathF.PI) a += 2 * MathF.PI;
            return a;
        }
    }

    /// <summary>Downscale source before perspective warp; also scales the quad in-place.</summary>
    private static SKBitmap DownscaleForWarp(SKBitmap src, ref SKPoint[] srcQuad, int maxDim)
    {
        int w = src.Width, h = src.Height;
        int curMax = Math.Max(w, h);
        if (curMax <= maxDim) return src; // no change

        float s = (float)maxDim / curMax;
        int nw = Math.Max(1, (int)Math.Round(w * s));
        int nh = Math.Max(1, (int)Math.Round(h * s));

        var resized = new SKBitmap(nw, nh, src.ColorType, src.AlphaType);
        using (var canvas = new SKCanvas(resized))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(src, new SKRect(0, 0, w, h), new SKRect(0, 0, nw, nh),
                              new SKPaint { FilterQuality = SKFilterQuality.Medium });
        }

        // Scale quad coordinates
        for (int i = 0; i < 4; i++)
            srcQuad[i] = new SKPoint(srcQuad[i].X * s, srcQuad[i].Y * s);

        return resized;
    }

    // ---------------- perspective warp (safe) ----------------

    private static SKBitmap WarpPerspectiveSafe(SKBitmap src, SKPoint[] srcQuad, int outW, int outH, SKColor background)
    {
        // Destination is a rectangle [0,outW]x[0,outH]
        var dstQuad = new[]
        {
            new SKPoint(0, 0),              // TL
            new SKPoint(outW - 1, 0),       // TR
            new SKPoint(outW - 1, outH - 1),// BR
            new SKPoint(0, outH - 1)        // BL
        };

        // Compute H that maps dst -> src (so we can sample src for each dst pixel)
        double[] H = ComputeHomography(dstQuad, srcQuad);

        // Inverse map each destination pixel into source, bilinear sample
        var result = new SKBitmap(outW, outH, src.ColorType, src.AlphaType);
        using var srcImg = SKImage.FromBitmap(src);
        using var srcPix = src.PeekPixels();

        for (int y = 0; y < outH; y++)
        {
            for (int x = 0; x < outW; x++)
            {
                var p = ApplyHomography(H, x + 0.5, y + 0.5); // center of pixel
                var c = BilinearSample(srcPix, p.X, p.Y, background);
                result.SetPixel(x, y, c);
            }
        }
        return result;
    }

    // --- math helpers for homography ---

    private static double[] ComputeHomography(SKPoint[] src, SKPoint[] dst)
    {
        // Solve 8 unknowns of 3x3 H (last element = 1). Use DLT with 4 point pairs.
        // src: p_i -> dst: q_i ; we solve H so that q ~ H p.
        // Here we directly compute H for mapping src->dst or dst->src based on how we pass arrays.

        // Build linear system A*h = b  (8x8)
        double[,] A = new double[8, 8];
        double[] b = new double[8];

        for (int i = 0; i < 4; i++)
        {
            double x = src[i].X, y = src[i].Y;
            double u = dst[i].X, v = dst[i].Y;

            int r = 2 * i;
            A[r, 0] = x; A[r, 1] = y; A[r, 2] = 1;
            A[r, 3] = 0; A[r, 4] = 0; A[r, 5] = 0;
            A[r, 6] = -x * u; A[r, 7] = -y * u;
            b[r] = u;

            A[r + 1, 0] = 0; A[r + 1, 1] = 0; A[r + 1, 2] = 0;
            A[r + 1, 3] = x; A[r + 1, 4] = y; A[r + 1, 5] = 1;
            A[r + 1, 6] = -x * v; A[r + 1, 7] = -y * v;
            b[r + 1] = v;
        }

        var h = Solve8(A, b); // 8 params; h8=1
        return new double[] { h[0], h[1], h[2], h[3], h[4], h[5], h[6], h[7], 1.0 };
    }

    private static SKPoint ApplyHomography(double[] H, double x, double y)
    {
        double X = H[0] * x + H[1] * y + H[2];
        double Y = H[3] * x + H[4] * y + H[5];
        double W = H[6] * x + H[7] * y + H[8];
        if (W == 0) return new SKPoint(0, 0);
        return new SKPoint((float)(X / W), (float)(Y / W));
    }

    private static SKColor BilinearSample(SKPixmap pixmap, double x, double y, SKColor outside)
    {
        int w = pixmap.Width, h = pixmap.Height;
        if (x < 0 || y < 0 || x >= w - 1 || y >= h - 1)
        {
            // simple clamp (or return 'outside' to get a hard edge with the background)
            int cx = Math.Clamp((int)Math.Round(x), 0, w - 1);
            int cy = Math.Clamp((int)Math.Round(y), 0, h - 1);
            return pixmap.GetPixelColor(cx, cy);
        }

        int x0 = (int)Math.Floor(x); int x1 = x0 + 1;
        int y0 = (int)Math.Floor(y); int y1 = y0 + 1;
        double fx = x - x0, fy = y - y0;

        var c00 = pixmap.GetPixelColor(x0, y0);
        var c10 = pixmap.GetPixelColor(x1, y0);
        var c01 = pixmap.GetPixelColor(x0, y1);
        var c11 = pixmap.GetPixelColor(x1, y1);

        byte lerp(byte a, byte b, double t) => (byte)Math.Round(a + (b - a) * t);

        var c0 = new SKColor(
            lerp(c00.Red, c10.Red, fx),
            lerp(c00.Green, c10.Green, fx),
            lerp(c00.Blue, c10.Blue, fx),
            lerp(c00.Alpha, c10.Alpha, fx));

        var c1 = new SKColor(
            lerp(c01.Red, c11.Red, fx),
            lerp(c01.Green, c11.Green, fx),
            lerp(c01.Blue, c11.Blue, fx),
            lerp(c01.Alpha, c11.Alpha, fx));

        return new SKColor(
            lerp(c0.Red, c1.Red, fy),
            lerp(c0.Green, c1.Green, fy),
            lerp(c0.Blue, c1.Blue, fy),
            lerp(c0.Alpha, c1.Alpha, fy));
    }

    // Solve 8x8 linear system by Gaussian elimination (tiny system; fine on CPU)
    private static double[] Solve8(double[,] A, double[] b)
    {
        int n = 8;
        double[,] M = new double[n, n + 1];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++) M[i, j] = A[i, j];
            M[i, n] = b[i];
        }

        for (int i = 0; i < n; i++)
        {
            // pivot
            int piv = i;
            for (int r = i + 1; r < n; r++)
                if (Math.Abs(M[r, i]) > Math.Abs(M[piv, i])) piv = r;
            if (piv != i)
                for (int c = i; c <= n; c++)
                {
                    double tmp = M[i, c]; M[i, c] = M[piv, c]; M[piv, c] = tmp;
                }

            double div = M[i, i];
            if (Math.Abs(div) < 1e-12) continue;
            for (int c = i; c <= n; c++) M[i, c] /= div;

            for (int r = 0; r < n; r++)
            {
                if (r == i) continue;
                double factor = M[r, i];
                for (int c = i; c <= n; c++)
                    M[r, c] -= factor * M[i, c];
            }
        }

        var x = new double[n];
        for (int i = 0; i < n; i++) x[i] = M[i, n];
        return x;
    }
}
