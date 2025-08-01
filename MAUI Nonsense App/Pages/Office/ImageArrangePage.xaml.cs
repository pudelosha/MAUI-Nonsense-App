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

    private void OnMoveUpClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ImagePageModel page)
            _viewModel.MoveUp(page);
    }

    private void OnMoveDownClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ImagePageModel page)
            _viewModel.MoveDown(page);
    }

    private async void OnNextClicked(object sender, EventArgs e)
    {
        _session.Pages = _viewModel.Pages.ToList();
        await Navigation.PushAsync(new SavePdfPage(_session));
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ImagePageModel page)
        {
            // Navigate to the image editor page with the selected image
            await Navigation.PushAsync(new ImageEditorPage(page));
        }
    }

}
