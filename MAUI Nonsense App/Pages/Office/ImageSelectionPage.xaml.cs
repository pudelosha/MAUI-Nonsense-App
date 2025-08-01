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
    }

    private async void OnPickPhotosClicked(object sender, EventArgs e)
    {
        await _viewModel.AddFromGalleryAsync();
    }

    private async void OnNextClicked(object sender, EventArgs e)
    {
        var session = new PdfCreationSession();
        session.Pages.AddRange(_viewModel.SelectedImages);
        await Navigation.PushAsync(new ImageArrangePage(session));
    }


    private void OnDeleteClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is ImagePageModel model)
            _viewModel.SelectedImages.Remove(model);
    }
}
