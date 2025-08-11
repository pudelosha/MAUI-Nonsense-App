using Microsoft.Maui.Controls;

namespace MAUI_Nonsense_App.Pages;

public partial class GamesPage : ContentPage
{
    public GamesPage()
    {
        InitializeComponent();
    }

    private async void OnSnakeTapped(object sender, EventArgs e)
    {
        // TODO: await Navigation.PushAsync(new SnakePage());
        await DisplayAlert("Snake", "Open Snake (coming soon)", "OK");
    }

    private async void OnTetrisTapped(object sender, EventArgs e)
    {
        // TODO: await Navigation.PushAsync(new TetrisPage());
        await DisplayAlert("Tetris", "Open Tetris (coming soon)", "OK");
    }

    private async void OnArkanoidTapped(object sender, EventArgs e)
    {
        // TODO: await Navigation.PushAsync(new ArkanoidPage());
        await DisplayAlert("Arkanoid", "Open Arkanoid (coming soon)", "OK");
    }

    private async void OnPongTapped(object sender, EventArgs e)
    {
        // TODO: await Navigation.PushAsync(new PongPage());
        await DisplayAlert("Pong", "Open Pong (coming soon)", "OK");
    }

    private async void OnSpaceInvadersTapped(object sender, EventArgs e)
    {
        // TODO: await Navigation.PushAsync(new SpaceInvadersPage());
        await DisplayAlert("Space Invaders", "Open Space Invaders (coming soon)", "OK");
    }

    private async void On2048Tapped(object sender, EventArgs e)
    {
        // TODO: await Navigation.PushAsync(new Game2048Page());
        await DisplayAlert("2048", "Open 2048 (coming soon)", "OK");
    }
}
