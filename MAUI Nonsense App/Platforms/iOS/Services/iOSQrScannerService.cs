using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Platforms.iOS.Services
{
    public class iOSQrScannerService : IQrScannerService
    {
        public Task<string?> ScanAsync()
        {
            // TODO: implement AVFoundation / ZXing
            return Task.FromResult<string?>("Scanned QR Code (iOS)");
        }
    }
}
