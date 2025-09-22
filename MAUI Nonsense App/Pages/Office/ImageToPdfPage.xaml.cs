using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;
using System.Collections.ObjectModel;

#if ANDROID
using Android.Widget;
#endif

namespace MAUI_Nonsense_App.Pages.Office;

public partial class ImageToPdfPage : ContentPage
{
    public ObservableCollection<PdfDocumentModel> PdfDocuments { get; set; } = new();

    private bool _subscribed;

    public ImageToPdfPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadPdfDocuments();

        if (!_subscribed)
        {
            _subscribed = true;
            MessagingCenter.Subscribe<SavePdfPage>(this, "PdfSaved", async _ =>
            {
#if ANDROID
                try { Toast.MakeText(Android.App.Application.Context, "PDF saved", ToastLength.Short)?.Show(); } catch { }
#else
                // Optional: await DisplayAlert("", "PDF saved", "OK");
#endif
                // Refresh again just in case save completed after navigation:
                LoadPdfDocuments();
            });
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_subscribed)
        {
            MessagingCenter.Unsubscribe<SavePdfPage>(this, "PdfSaved");
            _subscribed = false;
        }
    }

    private void LoadPdfDocuments()
    {
        PdfDocuments.Clear();

        string folder = FileSystem.AppDataDirectory;
        var files = Directory.EnumerateFiles(folder, "*.pdf")
                             .Select(p => new FileInfo(p))
                             .OrderByDescending(fi => fi.CreationTimeUtc); // newest → oldest

        foreach (var fi in files)
        {
            PdfDocuments.Add(new PdfDocumentModel
            {
                Name = fi.Name,
                FilePath = fi.FullName,
                SizeInBytes = fi.Length,
                CreatedAt = fi.CreationTime
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
        if ((sender as BindableObject)?.BindingContext is PdfDocumentModel pdf)
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
        if ((sender as BindableObject)?.BindingContext is PdfDocumentModel pdf)
        {
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Send PDF",
                File = new ShareFile(pdf.FilePath)
            });
        }
    }
}
