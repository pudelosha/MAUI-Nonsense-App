using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Services
{
    public interface IDocumentBuilderService
    {
        Task<string?> CapturePhotoAsync();
        Task<List<string>> PickImagesAsync();
        Task<string> SaveTempImageAsync(FileResult file);
        Task<bool> GeneratePdfAsync(PdfCreationSession session, string outputPath, string? password);
        Task<bool> CreatePdfAsync(string name, string? password, List<ImagePageModel> pages, int jpegQuality);
    }
}
