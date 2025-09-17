using System;
using System.IO;

namespace MAUI_Nonsense_App.Models
{
    /// <summary>
    /// Normalized point (0..1) in image coordinates.
    /// </summary>
    public readonly record struct PointD(double X, double Y)
    {
        public static readonly PointD Zero = new(0, 0);
    }

    /// <summary>
    /// Four-corner crop quad stored as normalized coordinates.
    /// Order: TL, TR, BR, BL.
    /// </summary>
    public record struct CropQuadNormalized(PointD TL, PointD TR, PointD BR, PointD BL)
    {
        public static readonly CropQuadNormalized FullImage =
            new(new PointD(0, 0), new PointD(1, 0), new PointD(1, 1), new PointD(0, 1));
    }

    public class ImagePageModel
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName => Path.GetFileName(FilePath);
        public DateTime CreatedAt { get; set; }
        public long FileSizeBytes { get; set; }
        public string Source { get; set; } = "Unknown"; // e.g., "Gallery" or "Camera"

        public string DisplayDate => CreatedAt.ToString("yyyy-MM-dd HH:mm");
        public string DisplaySize => $"{FileSizeBytes / 1024.0:F1} KB";

        /// <summary>
        /// User-defined frame crop, as normalized quad; null if not set yet.
        /// </summary>
        public CropQuadNormalized? FrameCrop { get; set; }
    }
}
