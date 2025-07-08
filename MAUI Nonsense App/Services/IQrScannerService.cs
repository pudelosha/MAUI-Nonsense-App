namespace MAUI_Nonsense_App.Services
{
    public interface IQrScannerService
    {
        Task<string?> ScanAsync();
    }
}
