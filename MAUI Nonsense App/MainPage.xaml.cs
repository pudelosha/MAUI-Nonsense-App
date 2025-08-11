using MAUI_Nonsense_App.Pages;
using MAUI_Nonsense_App.Pages.Activity;
using MAUI_Nonsense_App.Pages.Random;
using MAUI_Nonsense_App.Pages.Survival;

namespace MAUI_Nonsense_App
{
    public partial class MainPage : ContentPage
    {
        private readonly IServiceProvider _services;

        public MainPage(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
        }

        private async void OnStepCounterTapped(object sender, EventArgs e)
        {
            var page = _services.GetRequiredService<StepCounterPage>();
            await Navigation.PushAsync(page);
        }

        private async void OnSurvivalTapped(object sender, EventArgs e)
        {
            var page = _services.GetRequiredService<SurvivalPage>();
            await Navigation.PushAsync(page);
        }

        private async void OnToolsTapped(object sender, EventArgs e)
        {
            var page = _services.GetRequiredService<ToolsPage>();
            await Navigation.PushAsync(page);
        }

        private async void OnOfficeTapped(object sender, EventArgs e)
        {
            var page = _services.GetRequiredService<OfficePage>();
            await Navigation.PushAsync(page);
        }

        private async void OnGamesTapped(object sender, EventArgs e)
        {
            var page = _services.GetRequiredService<GamesPage>();
            await Navigation.PushAsync(page);
        }

        private async void OnRandomTapped(object sender, EventArgs e)
        {
            var page = _services.GetRequiredService<RandomPage>();
            await Navigation.PushAsync(page);
        }

        private async void OnProfileTapped(object sender, EventArgs e)
        {
            var page = _services.GetRequiredService<ProfilePage>();
            await Navigation.PushAsync(page);
        }

        private async void OnSettingsTapped(object sender, EventArgs e)
        {
            var page = _services.GetRequiredService<SettingsPage>();
            await Navigation.PushAsync(page);
        }
    }
}
