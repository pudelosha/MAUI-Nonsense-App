using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Platforms.Android.Services
{
    public class AndroidQrScannerService : IQrScannerService
    {
        public Task<string?> ScanAsync()
        {
            // TODO: implement ZXing.Net.Maui scanner page or intent
            // This is a stub
            return Task.FromResult<string?>("Scanned QR Code (Android)");
        }
    }
}
