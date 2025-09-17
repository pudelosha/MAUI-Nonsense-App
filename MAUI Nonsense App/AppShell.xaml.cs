namespace MAUI_Nonsense_App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register route to StepCounterPage.
            Routing.RegisterRoute(
                "stepcounter",
                typeof(MAUI_Nonsense_App.Pages.Activity.StepCounterPage));
        }
    }
}
