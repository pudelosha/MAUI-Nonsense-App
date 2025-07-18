using MAUI_Nonsense_App.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MAUI_Nonsense_App.Models;

public class RulerViewModel : INotifyPropertyChanged
{
    private readonly double _dpiY;
    private readonly double _density;

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _useInches;
    public string UnitToggleText => UseInches ? "Inches" : "Centimeters";

    public bool UseInches
    {
        get => _useInches;
        set
        {
            if (_useInches != value)
            {
                _useInches = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UnitLabel));
                OnPropertyChanged(nameof(DipsPerUnit));
                OnPropertyChanged(nameof(UnitToggleText));
            }
        }
    }

    public string UnitLabel => UseInches ? "in" : "cm";

    /// <summary>
    /// DIPs per unit (inches or cm)
    /// </summary>
    public double DipsPerUnit => UseInches
        ? (_dpiY / _density)
        : (_dpiY / _density) / 2.54;

    public ICommand SwitchUnitsCommand { get; }

    public RulerViewModel(IScreenMetricsService metricsService)
    {
        _dpiY = metricsService.DpiY;
        _density = metricsService.Density;

        Console.WriteLine($"[RulerViewModel] Detected dpiY = {_dpiY}, density = {_density}");

        _useInches = false; // default to cm
        SwitchUnitsCommand = new Command(SwitchUnits);
    }

    private void SwitchUnits()
    {
        UseInches = !UseInches;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
