using MAUI_Nonsense_App.Models;
using MAUI_Nonsense_App.Pages._Drawable;
using MAUI_Nonsense_App.Services;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Pages.Random;

public partial class DicePage : ContentPage
{
    private readonly DiceViewModel _viewModel;
    private readonly DiceDrawable _drawable;

    public DicePage(IDiceRollService diceRollService)
    {
        InitializeComponent();

        _viewModel = new DiceViewModel(diceRollService);
        _drawable = new DiceDrawable(_viewModel);
        DiceCanvas.Drawable = _drawable;
        BindingContext = _viewModel;

        // Hook into SizeChanged to capture canvas dimensions
        DiceCanvas.SizeChanged += (s, e) =>
        {
            var size = new Size(DiceCanvas.Width, DiceCanvas.Height);
            _viewModel.SetCanvasSize(size);
        };
    }

    private void OnDecreaseClicked(object sender, EventArgs e)
    {
        _viewModel.DecreaseDice();
        UpdateUI();
    }

    private void OnIncreaseClicked(object sender, EventArgs e)
    {
        _viewModel.IncreaseDice();
        UpdateUI();
    }

    private async void OnRollClicked(object sender, EventArgs e)
    {
        await _viewModel.AnimateRoll(DiceCanvas);

        var results = _viewModel.Animations.Select(d => d.Value).ToList();
        ResultLabel.Text = $"Result: {string.Join(", ", results)}";
        SumLabel.Text = $"Sum: {results.Sum()}";
    }

    private void UpdateUI()
    {
        DiceCanvas.Invalidate();
        DiceCountLabel.Text = $"{_viewModel.DiceCount} Dice{(_viewModel.DiceCount > 1 ? "s" : "")}";
    }
}
