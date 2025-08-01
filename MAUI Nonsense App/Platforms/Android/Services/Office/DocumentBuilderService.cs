using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;

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
            Console.WriteLine($"CapturePhotoAsync error: {ex.Message}");
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
            {
                var saved = await SaveTempImageAsync(photo);
                results.Add(saved);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PickImagesAsync error: {ex.Message}");
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

    public async Task<bool> GeneratePdfAsync(PdfCreationSession session, string outputPath, string? password)
    {
        try
        {
            // Simulate PDF generation — replace with real implementation later
            await Task.Delay(1000);

            File.WriteAllText(outputPath, $"PDF simulated with {session.Pages.Count} pages");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GeneratePdfAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreatePdfAsync(string name, string? password, List<ImagePageModel> pages)
    {
        try
        {
            // Placeholder: implement actual PDF generation here
            // For now, simulate saving
            await Task.Delay(500);

            // Save path logic can go here, depending on storage permissions

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating PDF: {ex.Message}");
            return false;
        }
    }
}
