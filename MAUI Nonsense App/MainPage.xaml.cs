using MAUI_Nonsense_App.Pages;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnStepCounterClicked(object sender, EventArgs e)
        {
            var stepService = App.Services.GetService<IStepCounterService>();
            if (stepService is null)
            {
                await DisplayAlert("Error", "Step counter service not available", "OK");
                return;
            }

            await Navigation.PushAsync(new StepCounterPage(stepService));
        }

        private async void OnQrScannerClicked(object sender, EventArgs e)
        {
            var qrService = App.Services.GetService<IQrScannerService>();
            if (qrService == null)
            {
                await DisplayAlert("Error", "QR scanner service not available.", "OK");
                return;
            }

            await Navigation.PushAsync(new QrScannerPage(qrService));
        }

    }
}
