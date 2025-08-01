using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;
using System.Collections.ObjectModel;

namespace MAUI_Nonsense_App.Pages.Office;

public partial class ImageToPdfPage : ContentPage
{
    public ObservableCollection<PdfDocumentModel> PdfDocuments { get; set; } = new();

    public ImageToPdfPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadPdfDocuments();
    }

    private void LoadPdfDocuments()
    {
        PdfDocuments.Clear();

        string folder = FileSystem.AppDataDirectory;
        var files = Directory.GetFiles(folder, "*.pdf");

        foreach (var path in files)
        {
            var info = new FileInfo(path);
            PdfDocuments.Add(new PdfDocumentModel
            {
                Name = Path.GetFileName(path),
                FilePath = path,
                SizeInBytes = info.Length,
                CreatedAt = info.CreationTime
            });
        }
    }

    private async void OnNewPdfClicked(object sender, EventArgs e)
    {
        var docService = App.Services.GetService<IDocumentBuilderService>()
            ?? throw new InvalidOperationException("DocumentBuilderService not available");

        await Navigation.PushAsync(new ImageSelectionPage(docService));
    }


    private async void OnDeletePdfClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is PdfDocumentModel pdf)
        {
            bool confirm = await DisplayAlert("Delete", $"Delete '{pdf.Name}'?", "Yes", "Cancel");
            if (confirm && File.Exists(pdf.FilePath))
            {
                File.Delete(pdf.FilePath);
                LoadPdfDocuments();
            }
        }
    }

    private async void OnSendPdfClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is PdfDocumentModel pdf)
        {
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Send PDF",
                File = new ShareFile(pdf.FilePath)
            });
        }
    }
}
