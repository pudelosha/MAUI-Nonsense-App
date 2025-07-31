using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Services;
using MAUI_Nonsense_App.Pages._Drawable;

namespace MAUI_Nonsense_App.Pages.Random;

public partial class RandomSpinnerPage : ContentPage
{
    private readonly RandomSpinnerViewModel _viewModel;
    private readonly SpinnerDrawable _drawable;

    public RandomSpinnerPage()
    {
        InitializeComponent();
        _viewModel = new RandomSpinnerViewModel();
        _drawable = new SpinnerDrawable(_viewModel);
        SpinnerCanvas.Drawable = _drawable;
        BindingContext = _viewModel;
    }

    private async void OnSpinClicked(object sender, EventArgs e)
    {
        await _viewModel.Spin(SpinnerCanvas);
        ResultLabel.Text = $"Result: {_viewModel.SelectedOption}";
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        string current = string.Join(",", _viewModel.Options);
        string input = await DisplayPromptAsync("Edit Options",
            "Enter comma-separated values (2–20):", initialValue: current);

        if (!string.IsNullOrWhiteSpace(input))
        {
            var newOptions = input
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (newOptions.Count >= 2 && newOptions.Count <= 20)
            {
                _viewModel.SetOptions(newOptions);
                SpinnerCanvas.Invalidate();
            }
            else
            {
                await DisplayAlert("Invalid", "Please enter between 2 and 20 options.", "OK");
            }
        }
    }
}
