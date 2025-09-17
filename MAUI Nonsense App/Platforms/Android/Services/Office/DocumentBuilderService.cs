using AndroidX.ExifInterface.Media;
using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;
using SkiaSharp;

namespace MAUI_Nonsense_App.Platforms.Android.Services.Office;

public class DocumentBuilderService : IDocumentBuilderService
{
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

    // Legacy helper – left for compatibility; now uses the real writer
    public async Task<bool> GeneratePdfAsync(PdfCreationSession session, string outputPath, string? password)
        => await CreatePdfInternalAsync(outputPath, session.Pages);

    public async Task<bool> CreatePdfAsync(string name, string? password, List<ImagePageModel> pages)
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
            var ok = await CreatePdfInternalAsync(path, pages);
            return ok;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CreatePdfAsync error: {ex.Message}");
            return false;
        }
    }

    // ----------------- internals -----------------

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
    /// Core PDF writer. Uses SkiaSharp to crop images (axis-aligned) and add as PDF pages.
    /// Note: SkiaSharp's PDF doesn't support password; 'password' is currently ignored.
    /// </summary>
    private static async Task<bool> CreatePdfInternalAsync(string outputPath, List<ImagePageModel> pages)
    {
        try
        {
            using var fs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var doc = SKDocument.CreatePdf(fs);

            foreach (var p in pages)
            {
                if (!File.Exists(p.FilePath)) continue;

                using var upright = LoadBitmapUpright(p.FilePath);
                if (upright is null) continue;

                var crop = ToCropRect(upright.Width, upright.Height, p.FrameCrop);

                // Clamp to bounds
                crop = SKRectI.Intersect(crop, new SKRectI(0, 0, upright.Width, upright.Height));
                if (crop.IsEmpty) continue;

                // Crop
                using var cropped = new SKBitmap(crop.Width, crop.Height, upright.ColorType, upright.AlphaType);
                using (var canvas = new SKCanvas(cropped))
                {
                    var src = new SKRect(crop.Left, crop.Top, crop.Right, crop.Bottom);
                    var dst = new SKRect(0, 0, crop.Width, crop.Height);
                    canvas.DrawBitmap(upright, src, dst);
                }

                // PDF page size = cropped bitmap size (points)
                using var pageCanvas = doc.BeginPage(cropped.Width, cropped.Height);
                pageCanvas.DrawBitmap(cropped, new SKRect(0, 0, cropped.Width, cropped.Height));
                doc.EndPage();
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

    /// <summary>
    /// Load bitmap and rotate/flip to upright based on EXIF orientation (Android).
    /// </summary>
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

            // EXIF
            int degrees = 0;
            bool flipH = false, flipV = false;
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

            if (degrees == 0 && !flipH && !flipV)
                return bmp;

            // Transform to upright
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

    /// <summary>
    /// Convert a normalized quad (or null) into an axis-aligned pixel rect.
    /// </summary>
    private static SKRectI ToCropRect(int width, int height, CropQuadNormalized? quad)
    {
        if (quad is null)
            return new SKRectI(0, 0, width, height);

        var q = quad.Value;

        int x0 = (int)Math.Round(q.TL.X * width);
        int y0 = (int)Math.Round(q.TL.Y * height);
        int x1 = (int)Math.Round(q.TR.X * width);
        int y1 = (int)Math.Round(q.TR.Y * height);
        int x2 = (int)Math.Round(q.BR.X * width);
        int y2 = (int)Math.Round(q.BR.Y * height);
        int x3 = (int)Math.Round(q.BL.X * width);
        int y3 = (int)Math.Round(q.BL.Y * height);

        int left = Math.Clamp(Math.Min(Math.Min(x0, x1), Math.Min(x2, x3)), 0, width);
        int top = Math.Clamp(Math.Min(Math.Min(y0, y1), Math.Min(y2, y3)), 0, height);
        int right = Math.Clamp(Math.Max(Math.Max(x0, x1), Math.Max(x2, x3)), 0, width);
        int bottom = Math.Clamp(Math.Max(Math.Max(y0, y1), Math.Max(y2, y3)), 0, height);

        if (right <= left) right = Math.Min(left + 1, width);
        if (bottom <= top) bottom = Math.Min(top + 1, height);

        return new SKRectI(left, top, right, bottom);
    }
}
