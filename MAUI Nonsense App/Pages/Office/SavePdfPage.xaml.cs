using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;
using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages.Office;

public partial class SavePdfPage : ContentPage
{
    private readonly SavePdfViewModel _viewModel;
    private readonly PdfCreationSession _session;

    public SavePdfPage(PdfCreationSession session)
    {
        InitializeComponent();
        _session = session;

        var docService = App.Services.GetService<IDocumentBuilderService>()!;
        _viewModel = new SavePdfViewModel(docService);
        BindingContext = _viewModel;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var fileName = _viewModel.GetSafeFileName();
        var password = _viewModel.Password;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            await DisplayAlert("Missing Name", "Please enter a document name.", "OK");
            return;
        }

        bool ok = await _viewModel.SaveDocumentAsync(fileName, password, _session.Pages);

        if (ok)
        {
            // ImageToPdfPage is your root; it refreshes in OnAppearing()
            await Navigation.PopToRootAsync();
        }
        else
        {
            await DisplayAlert("Error", "Failed to save PDF.", "OK");
        }
    }
}
