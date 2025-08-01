using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MAUI_Nonsense_App.ViewModels;

public class ImageSelectionViewModel : INotifyPropertyChanged
{
    private readonly IDocumentBuilderService _docService;

    public ObservableCollection<ImagePageModel> SelectedImages { get; } = new();

    public ImageSelectionViewModel(IDocumentBuilderService documentBuilderService)
    {
        _docService = documentBuilderService;
    }

    public async Task AddFromCameraAsync()
    {
        var photoPath = await _docService.CapturePhotoAsync();
        if (!string.IsNullOrWhiteSpace(photoPath))
        {
            var fileInfo = new FileInfo(photoPath);
            SelectedImages.Add(new ImagePageModel
            {
                FilePath = photoPath,
                Source = "Camera",
                CreatedAt = fileInfo.CreationTime,
                FileSizeBytes = fileInfo.Length
            });
        }
    }

    public async Task AddFromGalleryAsync()
    {
        var images = await _docService.PickImagesAsync();
        foreach (var img in images)
        {
            var fileInfo = new FileInfo(img);
            SelectedImages.Add(new ImagePageModel
            {
                FilePath = img,
                Source = "Gallery",
                CreatedAt = fileInfo.CreationTime,
                FileSizeBytes = fileInfo.Length
            });
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
