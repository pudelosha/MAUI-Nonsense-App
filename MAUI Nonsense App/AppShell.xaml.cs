namespace MAUI_Nonsense_App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("imagetopdf", typeof(MAUI_Nonsense_App.Pages.Office.ImageToPdfPage));
            Routing.RegisterRoute("selectimages", typeof(MAUI_Nonsense_App.Pages.Office.ImageSelectionPage));
            Routing.RegisterRoute("arrangepages", typeof(MAUI_Nonsense_App.Pages.Office.ImageArrangePage));
            Routing.RegisterRoute("editimage", typeof(MAUI_Nonsense_App.Pages.Office.ImageEditorPage));
            Routing.RegisterRoute("savepdf", typeof(MAUI_Nonsense_App.Pages.Office.SavePdfPage));

            Routing.RegisterRoute("stepcounter", typeof(MAUI_Nonsense_App.Pages.Activity.StepCounterPage));
        }
    }
}
