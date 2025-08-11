using MAUI_Nonsense_App.ViewModels;

namespace MAUI_Nonsense_App.Pages
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            BindingContext = new SettingsViewModel();
        }
    }
}
