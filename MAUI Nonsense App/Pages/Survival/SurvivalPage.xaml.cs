namespace MAUI_Nonsense_App.Pages.Survival;

public partial class SurvivalPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public SurvivalPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    private async void OnLightTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<LightPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }

    private async void OnCompassTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<CompassPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }

    private async void OnMovementDetectorTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<MovementDetectorPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }

    private async void OnMarkPointTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<MarkPointPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }

    private async void OnChecklistTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<ChecklistPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }
}
