using MAUI_Nonsense_App.Pages.Office;
using MAUI_Nonsense_App.Pages.Tools;

namespace MAUI_Nonsense_App.Pages;

public partial class OfficePage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public OfficePage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    private async void OnImageToPdfTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<ImageToPdfPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }

    private async void OnImageToTextTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<ImageToTextPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }

    private async void OnQrScannerTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<QrScannerPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }
}
