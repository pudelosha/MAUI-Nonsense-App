using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;
using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages.Office;

public partial class ImageSelectionPage : ContentPage
{
    private readonly ImageSelectionViewModel _viewModel;

    public ImageSelectionPage(IDocumentBuilderService docService)
    {
        InitializeComponent();
        _viewModel = new ImageSelectionViewModel(docService);
        BindingContext = _viewModel;
    }

    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        await _viewModel.AddFromCameraAsync();
        InitializeNewItemsMetadata();
    }

    private async void OnPickPhotosClicked(object sender, EventArgs e)
    {
        await _viewModel.AddFromGalleryAsync();
        InitializeNewItemsMetadata();
    }

    private async void OnNextClicked(object sender, EventArgs e)
    {
        // Make sure every item has pixel size + default crop
        InitializeNewItemsMetadata();

        var session = new PdfCreationSession();
        session.Pages.AddRange(_viewModel.SelectedImages);
        await Navigation.PushAsync(new ImageArrangePage(session));
    }

    private void OnDeleteClicked(object sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is ImagePageModel model)
            _viewModel.SelectedImages.Remove(model);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    /// <summary>
    /// Fills missing metadata (pixel size, dates, file length) and seeds FrameCrop = FullImage
    /// for any newly added images.
    /// </summary>
    private void InitializeNewItemsMetadata()
    {
        foreach (var m in _viewModel.SelectedImages)
        {
            if (string.IsNullOrWhiteSpace(m.FilePath) || !File.Exists(m.FilePath))
                continue;

            // Pixel size
            if (m.OriginalPixelWidth == 0 || m.OriginalPixelHeight == 0)
            {
                try
                {
                    using var fs = File.OpenRead(m.FilePath);
#if ANDROID || IOS || MACCATALYST || WINDOWS
                    var img = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(fs);
                    m.OriginalPixelWidth = (int)img.Width;
                    m.OriginalPixelHeight = (int)img.Height;
#else
                    m.OriginalPixelWidth = 1000;
                    m.OriginalPixelHeight = 1000;
#endif
                }
                catch
                {
                    m.OriginalPixelWidth = 1000;
                    m.OriginalPixelHeight = 1000;
                }
            }

            // Default full-image crop (only if not set by editor previously)
            m.FrameCrop ??= CropQuadNormalized.FullImage;

            // Basic metadata (handy for UI)
            if (m.CreatedAt == default)
                m.CreatedAt = File.GetCreationTime(m.FilePath);

            if (m.FileSizeBytes == 0)
                m.FileSizeBytes = new FileInfo(m.FilePath).Length;

            if (string.IsNullOrWhiteSpace(m.Source))
                m.Source = "Gallery";
        }
    }
}
