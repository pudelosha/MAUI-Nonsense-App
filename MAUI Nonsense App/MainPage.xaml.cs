using MAUI_Nonsense_App.Pages;

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
            await Navigation.PushAsync(new StepCounterPage());
        }

        private async void OnQrScannerClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new QrScannerPage());
        }
    }
}
