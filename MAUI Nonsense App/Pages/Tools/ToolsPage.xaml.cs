using MAUI_Nonsense_App.Pages.Tools;

namespace MAUI_Nonsense_App.Pages;

public partial class ToolsPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public ToolsPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    private async void OnLevelTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<LevelPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }

    private async void OnUnitConverterTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<UnitConverterPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }

    private async void OnRulerTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<RulerPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }

    private async void OnProtractorTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<ProtractorPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }
}
