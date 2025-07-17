using MAUI_Nonsense_App.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MAUI_Nonsense_App.Models;

public class LevelViewModel : INotifyPropertyChanged
{
    private readonly ILevelService _levelService;

    public event PropertyChangedEventHandler? PropertyChanged;

    private double _pitch;
    public double Pitch
    {
        get => _pitch;
        private set
        {
            if (_pitch != value)
            {
                _pitch = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TiltY));
            }
        }
    }

    private double _roll;
    public double Roll
    {
        get => _roll;
        private set
        {
            if (_roll != value)
            {
                _roll = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TiltX));
            }
        }
    }

    // Normalized values: -1..1
    public double TiltX => Math.Clamp(Roll / 45.0, -1, 1);
    public double TiltY => Math.Clamp(Pitch / 45.0, -1, 1);

    public LevelViewModel(ILevelService levelService)
    {
        _levelService = levelService;
        _levelService.ReadingChanged += OnReadingChanged;
    }

    private DateTime _lastUpdate = DateTime.MinValue;

    private void OnReadingChanged(object? sender, LevelReading e)
    {
        var now = DateTime.UtcNow;

        // Throttle updates: only refresh every 200ms
        if ((now - _lastUpdate).TotalMilliseconds < 200)
            return;

        _lastUpdate = now;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Pitch = e.Pitch;
            Roll = e.Roll;
        });
    }

    public async Task StartAsync() => await _levelService.StartAsync();
    public async Task StopAsync() => await _levelService.StopAsync();

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
