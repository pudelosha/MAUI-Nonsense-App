using MAUI_Nonsense_App.Pages;
using MAUI_Nonsense_App.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using ZXing.Net.Maui.Readers;

namespace MAUI_Nonsense_App.Platforms.Android.Services
{
    public class AndroidQrScannerService : IQrScannerService
    {
        public Task<string?> ScanAsync()
        {
            // not needed because we delegate to QrScannerViewModel
            return Task.FromResult<string?>(null);
        }
    }
}
