using System.ComponentModel;
using System.Windows.Input;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Models
{
    public class QrScannerViewModel : INotifyPropertyChanged
    {
        private readonly IQrScannerService _qrService;

        private string? _result;
        public string? Result
        {
            get => _result;
            set
            {
                if (_result != value)
                {
                    _result = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
                }
            }
        }

        public ICommand ScanCommand { get; }

        public QrScannerViewModel(IQrScannerService qrService)
        {
            _qrService = qrService;
            ScanCommand = new Command(async () => await ScanAsync());
        }

        private async Task ScanAsync()
        {
            Result = await _qrService.ScanAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
