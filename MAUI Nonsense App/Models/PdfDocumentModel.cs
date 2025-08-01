namespace MAUI_Nonsense_App.Models;

public class PdfDocumentModel
{
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long SizeInBytes { get; set; }
    public DateTime CreatedAt { get; set; }

    public string SizeInKb => $"{SizeInBytes / 1024.0:F1} KB";
}
