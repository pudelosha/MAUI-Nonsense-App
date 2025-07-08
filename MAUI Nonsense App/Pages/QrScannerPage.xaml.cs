using MAUI_Nonsense_App.Services;
using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages;

public partial class QrScannerPage : ContentPage
{
    public QrScannerPage(IQrScannerService qrService)
    {
        InitializeComponent();

        BindingContext = new QrScannerViewModel(qrService);
    }
}
