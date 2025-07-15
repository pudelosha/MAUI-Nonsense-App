using MAUI_Nonsense_App.Pages;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnStepCounterTapped(object sender, EventArgs e) =>
            await Navigation.PushAsync(new StepCounterPage(App.Services.GetService<IStepCounterService>()));

        private async void OnSurvivalTapped(object sender, EventArgs e) =>
            await Navigation.PushAsync(new SurvivalPage());

        private async void OnToolsTapped(object sender, EventArgs e) =>
            await Navigation.PushAsync(new ToolsPage());

        private async void OnOfficeTapped(object sender, EventArgs e) =>
            await Navigation.PushAsync(new OfficePage());

        private async void OnFinanceTapped(object sender, EventArgs e) =>
            await Navigation.PushAsync(new FinancePage());

        private async void OnScannerTapped(object sender, EventArgs e) =>
            await Navigation.PushAsync(new QrScannerPage());

        private async void OnProfileTapped(object sender, EventArgs e) =>
            await Navigation.PushAsync(new ProfilePage());

        private async void OnSettingsTapped(object sender, EventArgs e) =>
            await Navigation.PushAsync(new SettingsPage());
    }
}
