using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Models;

public partial class QrScannerViewModel
{
    private readonly IQrScannerService _qrService;

    public string Result { get; set; }

    public QrScannerViewModel(IQrScannerService qrService)
    {
        _qrService = qrService;
    }

    public void OnBarcodeDetected(string? result)
    {
        Result = result ?? "No result";
    }
}
