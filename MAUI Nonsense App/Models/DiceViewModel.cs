using System.ComponentModel;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Models;

public class DiceViewModel : INotifyPropertyChanged
{
    private readonly IDiceRollService _diceRollService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int DiceCount { get; private set; } = 1;

    public List<int> RollResults { get; private set; } = new() { 1 };

    public DiceViewModel(IDiceRollService diceRollService)
    {
        _diceRollService = diceRollService;
    }

    public void IncreaseDice()
    {
        if (DiceCount < 3)
            DiceCount++;
    }

    public void DecreaseDice()
    {
        if (DiceCount > 1)
            DiceCount--;
    }

    public async Task RollDicesAsync()
    {
        RollResults = await _diceRollService.RollAsync(DiceCount);
        OnPropertyChanged(nameof(RollResults));
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
