namespace MAUI_Nonsense_App.Models;

public class QrScannerViewModel
{
    public string Result { get; private set; }

    public QrScannerViewModel()
    {
        Result = "Please point the camera at a QR or barcode";
    }

    public void OnBarcodeDetected(string? result)
    {
        Result = result ?? "No result";
    }

    public void ResetResult()
    {
        Result = "Please point the camera at a QR or barcode";
    }
}
