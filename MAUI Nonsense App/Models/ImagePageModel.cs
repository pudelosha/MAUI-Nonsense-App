namespace MAUI_Nonsense_App.Models
{
    public class ImagePageModel
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName => Path.GetFileName(FilePath);
        public DateTime CreatedAt { get; set; }
        public long FileSizeBytes { get; set; }
        public string Source { get; set; } = "Unknown"; // e.g., "Gallery" or "Camera"

        public string DisplayDate => CreatedAt.ToString("yyyy-MM-dd HH:mm");
        public string DisplaySize => $"{FileSizeBytes / 1024.0:F1} KB";
    }
}
