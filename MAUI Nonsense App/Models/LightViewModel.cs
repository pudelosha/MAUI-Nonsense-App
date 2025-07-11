﻿using MAUI_Nonsense_App.Helpers;
using MAUI_Nonsense_App.Services;
using System.ComponentModel;

public class LightViewModel : INotifyPropertyChanged
{
    private readonly ILightService _lightService;

    public event PropertyChangedEventHandler PropertyChanged;

    public bool IsOn { get; private set; }
    public bool IsStrobeOn { get; private set; }
    public bool IsSOSOn { get; private set; }
    public bool IsMorseOn { get; private set; }
    public bool IsLighthouseOn { get; private set; }
    public bool IsPoliceOn { get; private set; }


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

    public async Task ToggleLighthouseAsync()
    {
        if (!IsLighthouseOn)
        {
            await StopAllModes();
            await _lightService.StartLighthouseAsync();
            IsLighthouseOn = true;
        }
        else
        {
            await _lightService.StopLighthouseAsync();
            IsLighthouseOn = false;
        }

        OnPropertyChanged(nameof(IsLighthouseOn));
    }

    public async Task TogglePoliceAsync()
    {
        if (!IsPoliceOn)
        {
            await StopAllModes();
            await _lightService.StartPoliceAsync();
            IsPoliceOn = true;
        }
        else
        {
            await _lightService.StopPoliceAsync();
            IsPoliceOn = false;
        }

        OnPropertyChanged(nameof(IsPoliceOn));
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

    public async Task SendMorseMessageAsync(string message)
    {
        await StopAllModes();
        IsMorseOn = true;
        OnPropertyChanged(nameof(IsMorseOn));

        _ = Task.Run(async () =>
        {
            var morse = MorseEncoder.Encode(message);
            await _lightService.StartMorseAsync(morse);
            IsMorseOn = false;
            OnPropertyChanged(nameof(IsMorseOn));
        });
    }

    private async Task StopAllModes()
    {
        if (IsOn)
        {
            await _lightService.TurnOffAsync();
            IsOn = false;
            OnPropertyChanged(nameof(IsOn));
        }

        if (IsLighthouseOn)
        {
            await _lightService.StopLighthouseAsync();
            IsLighthouseOn = false;
            OnPropertyChanged(nameof(IsLighthouseOn));
        }

        if (IsPoliceOn)
        {
            await _lightService.StopPoliceAsync();
            IsPoliceOn = false;
            OnPropertyChanged(nameof(IsPoliceOn));
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

        if (IsMorseOn)
        {
            await _lightService.StopSOSAsync();
            IsMorseOn = false;
            OnPropertyChanged(nameof(IsMorseOn));
        }

        await _lightService.TurnOffAsync();
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
