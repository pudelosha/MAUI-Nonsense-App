using MAUI_Nonsense_App.Models;

namespace MAUI_Nonsense_App.Pages;

public partial class SendMorseModal : ContentPage
{
    private readonly LightViewModel _vm;

    public SendMorseModal(LightViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        WarningLabel.IsVisible = e.NewTextValue.Length > 100;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (MessageEntry.Text.Length > 100) return;

        await _vm.SendMorseMessageAsync(MessageEntry.Text);
        await Navigation.PopModalAsync();
    }
}
