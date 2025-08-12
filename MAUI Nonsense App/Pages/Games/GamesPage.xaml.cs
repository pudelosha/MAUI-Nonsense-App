using Microsoft.Maui.Controls;

namespace MAUI_Nonsense_App.Pages;

public partial class GamesPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider; // already in your parent page

    public GamesPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    private async void OnSnakeTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<MAUI_Nonsense_App.Pages.Games.SnakePage>();
        if (page is not null)
        {
            await Navigation.PushAsync(page);
        }
        else
        {
            await DisplayAlert("Snake", "SnakePage isn't registered in DI.", "OK");
        }
    }

    private async void OnTetrisTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<MAUI_Nonsense_App.Pages.Games.TetrisPage>();
        if (page is not null)
        {
            await Navigation.PushAsync(page);
        }
        else
        {
            await DisplayAlert("Tetris", "TetrisPage isn't registered in DI.", "OK");
        }
    }

    private async void OnArkanoidTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<MAUI_Nonsense_App.Pages.Games.ArkanoidPage>();
        if (page is not null)
        {
            await Navigation.PushAsync(page);
        }
        else
        {
            await DisplayAlert("Arkanoid", "ArkanoidPage isn't registered in DI.", "OK");
        }
    }

    private async void OnPongTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<MAUI_Nonsense_App.Pages.Games.PongGame>();
        if (page is not null)
        {
            await Navigation.PushAsync(page);
        }
        else
        {
            await DisplayAlert("Pong", "PongGame isn't registered in DI.", "OK");
        }
    }

    private async void OnSpaceInvadersTapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<MAUI_Nonsense_App.Pages.Games.SpaceInvadersPage>();
        if (page is not null)
        {
            await Navigation.PushAsync(page);
        }
        else
        {
            await DisplayAlert("SpaceInvaders", "SpaceInvadersPage isn't registered in DI.", "OK");
        }
    }

    private async void On2048Tapped(object sender, EventArgs e)
    {
        var page = _serviceProvider.GetService<MAUI_Nonsense_App.Pages.Games._2048Page>();
        if (page is not null)
        {
            await Navigation.PushAsync(page);
        }
        else
        {
            await DisplayAlert("2048", "2048Page isn't registered in DI.", "OK");
        }
    }
}
