using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages;

public partial class QrScannerPage : ContentPage
{
    private readonly QrScannerViewModel _viewModel;

    public QrScannerPage(QrScannerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private void BarcodesDetected(object sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault()?.Value;

        if (!string.IsNullOrEmpty(result))
        {
            _viewModel.OnBarcodeDetected(result);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Scanned", result, "OK");
                await Navigation.PopAsync();
            });
        }
    }
}
