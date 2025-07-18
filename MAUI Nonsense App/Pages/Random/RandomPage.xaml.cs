namespace MAUI_Nonsense_App.Pages.Random;

public partial class RandomPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public RandomPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    private async void OnRandomNumberTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<RandomNumberPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }

    private async void OnCoinFlipTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<CoinFlipPage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }

    private async void OnDiceTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<DicePage>();
        if (page is not null)
            await Navigation.PushAsync(page);
    }
}
