using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.ViewModels;

public class ImageSelectionViewModel : INotifyPropertyChanged
{
    private readonly IDocumentBuilderService _docService;
    public ObservableCollection<string> SelectedImages { get; } = new();

    public ImageSelectionViewModel(IDocumentBuilderService documentBuilderService)
    {
        _docService = documentBuilderService;
    }

    public async Task AddFromCameraAsync()
    {
        var photo = await _docService.CapturePhotoAsync();
        if (!string.IsNullOrWhiteSpace(photo))
            SelectedImages.Add(photo);
    }

    public async Task AddFromGalleryAsync()
    {
        var images = await _docService.PickImagesAsync();
        foreach (var img in images)
            SelectedImages.Add(img);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
