using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        public App(IServiceProvider services, IStepCounterService stepCounterService)
        {
            InitializeComponent();

            Services = services;

            MainPage = new NavigationPage(new MainPage(services));

            stepCounterService.StartAsync();
        }
    }
}
