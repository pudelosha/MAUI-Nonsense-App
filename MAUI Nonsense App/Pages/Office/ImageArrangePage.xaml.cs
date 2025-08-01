using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages.Office;

public partial class ImageArrangePage : ContentPage
{
    private readonly ImageArrangeViewModel _viewModel;
    private readonly PdfCreationSession _session;

    public ImageArrangePage(PdfCreationSession session)
    {
        InitializeComponent();
        _session = session;
        _viewModel = new ImageArrangeViewModel(session);
        BindingContext = _viewModel;
    }

    private async void OnNextClicked(object sender, EventArgs e)
    {
        _session.Pages = _viewModel.Pages.ToList();
        await Navigation.PushAsync(new SavePdfPage(_session));
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnMoveUpTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is ImagePageModel page)
            _viewModel.MoveUp(page);
    }

    private void OnMoveDownTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is ImagePageModel page)
            _viewModel.MoveDown(page);
    }

    private async void OnEditTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is ImagePageModel page)
            await Navigation.PushAsync(new ImageEditorPage(page));
    }
}
