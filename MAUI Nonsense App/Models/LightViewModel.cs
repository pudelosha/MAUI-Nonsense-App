using MAUI_Nonsense_App.Services;
using System.ComponentModel;

public class LightViewModel : INotifyPropertyChanged
{
    private readonly ILightService _lightService;

    public event PropertyChangedEventHandler PropertyChanged;

    public bool IsOn { get; private set; }
    public bool IsStrobeOn { get; private set; }
    public bool IsSOSOn { get; private set; }

    public LightViewModel(ILightService lightService)
    {
        _lightService = lightService;
    }

    public async Task ToggleLightAsync()
    {
        if (!IsOn)
        {
            await StopAllModes();
            await _lightService.TurnOnAsync();
            IsOn = true;
        }
        else
        {
            await _lightService.TurnOffAsync();
            IsOn = false;
        }

        OnPropertyChanged(nameof(IsOn));
    }

    public async Task ToggleStrobeAsync()
    {
        if (!IsStrobeOn)
        {
            await StopAllModes();
            await _lightService.StartStrobeAsync(100);
            IsStrobeOn = true;
        }
        else
        {
            await _lightService.StopStrobeAsync();
            IsStrobeOn = false;
        }

        OnPropertyChanged(nameof(IsStrobeOn));
    }

    public async Task ToggleSOSAsync()
    {
        if (!IsSOSOn)
        {
            await StopAllModes();
            IsSOSOn = true;
            OnPropertyChanged(nameof(IsSOSOn));

            _ = _lightService.StartSOSAsync(); // fire and forget
        }
        else
        {
            await _lightService.StopSOSAsync();
            IsSOSOn = false;
            OnPropertyChanged(nameof(IsSOSOn));
        }
    }

    private async Task StopAllModes()
    {
        if (IsOn)
        {
            await _lightService.TurnOffAsync();
            IsOn = false;
            OnPropertyChanged(nameof(IsOn));
        }

        if (IsStrobeOn)
        {
            await _lightService.StopStrobeAsync();
            IsStrobeOn = false;
            OnPropertyChanged(nameof(IsStrobeOn));
        }

        if (IsSOSOn)
        {
            await _lightService.StopSOSAsync();
            IsSOSOn = false;
            OnPropertyChanged(nameof(IsSOSOn));
        }

        await _lightService.TurnOffAsync();
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
