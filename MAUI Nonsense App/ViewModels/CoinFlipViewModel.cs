using System.ComponentModel;
using System.Windows.Input;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.ViewModels;

public class CoinFlipViewModel : INotifyPropertyChanged
{
    private readonly ICoinFlipService _coinFlipService;

    public event PropertyChangedEventHandler? PropertyChanged;

    string _resultText = "Press the button to toss the coin";
    double _currentAngle = 0;
    string _face = "Eagle";

    public string ResultText
    {
        get => _resultText;
        set { _resultText = value; OnPropertyChanged(nameof(ResultText)); }
    }

    public double CurrentAngle
    {
        get => _currentAngle;
        set { _currentAngle = value; OnPropertyChanged(nameof(CurrentAngle)); }
    }

    public string Face
    {
        get => _face;
        set { _face = value; OnPropertyChanged(nameof(Face)); }
    }

    public ICommand TossCommand { get; }

    public CoinFlipViewModel(ICoinFlipService coinFlipService)
    {
        _coinFlipService = coinFlipService;
        TossCommand = new Command(OnToss);
    }

    private void OnToss()
    {
        var isEagle = _coinFlipService.Toss();
        Face = isEagle ? "Eagle" : "1";
        ResultText = $"Result: {Face}";
        CurrentAngle = 0; // reset angle here, animation should update it
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
