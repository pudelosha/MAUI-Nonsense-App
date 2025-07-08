using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages;

public partial class QrScannerPage : ContentPage
{
    private readonly QrScannerViewModel _viewModel;

    public QrScannerPage()
    {
        InitializeComponent();

        _viewModel = new QrScannerViewModel();
        BindingContext = _viewModel;

        UpdateResultLabel();
    }

    private void BarcodesDetected(object sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault()?.Value;

        if (!string.IsNullOrEmpty(result))
        {
            CameraBarcodeReaderView.IsDetecting = false;

            _viewModel.OnBarcodeDetected(result);
            UpdateResultLabel();
        }
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        _viewModel.ResetResult();
        UpdateResultLabel();

        CameraBarcodeReaderView.IsDetecting = true;
    }

    private void UpdateResultLabel()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ResultLabel.Text = _viewModel.Result;
        });
    }
}
