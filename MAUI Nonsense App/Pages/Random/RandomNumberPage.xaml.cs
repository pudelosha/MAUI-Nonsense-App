using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages.Random;

public partial class RandomNumberPage : ContentPage
{
    public RandomNumberPage(RandomNumberViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}