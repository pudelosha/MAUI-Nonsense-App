﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using Foundation;
using MAUI_Nonsense_App.Services;

[assembly: Microsoft.Maui.Controls.Dependency(typeof(MAUI_Nonsense_App.Platforms.iOS.Services.Light.iOSLightService))]

namespace MAUI_Nonsense_App.Platforms.iOS.Services.Light
{
    public class iOSLightService : ILightService
    {
        private readonly AVCaptureDevice? _device;
        private CancellationTokenSource? _cts;

        public bool IsOn { get; private set; }

        public iOSLightService()
        {
            _device = AVCaptureDevice
                        .DevicesWithMediaType("vide")  // ⬅️ kept exactly as your original
                        .FirstOrDefault(d => d.HasTorch);

            if (_device == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No camera with torch available.");
            }
        }

        public Task<bool> IsSupportedAsync() =>
            Task.FromResult(_device?.TorchAvailable ?? false);

        public async Task TurnOnAsync()
        {
            if (_device == null || !_device.TorchAvailable) return;

            _device.LockForConfiguration(out var error);
            if (error == null)
            {
                _device.TorchMode = AVCaptureTorchMode.On;
                _device.UnlockForConfiguration();
                IsOn = true;
            }
        }

        public async Task TurnOffAsync()
        {
            if (_device == null || !_device.TorchAvailable) return;

            _device.LockForConfiguration(out var error);
            if (error == null)
            {
                _device.TorchMode = AVCaptureTorchMode.Off;
                _device.UnlockForConfiguration();
                IsOn = false;
            }
        }

        public Task SetBrightnessAsync(double strength)
        {
            if (_device == null || !_device.HasTorch) return Task.CompletedTask;

            _device.LockForConfiguration(out var error);
            if (error == null)
            {
                float level = (float)Math.Clamp(strength, 0.01, 1.0);
                _device.SetTorchModeLevel(level, out var levelError);
                _device.UnlockForConfiguration();
            }

            return Task.CompletedTask;
        }

        public Task StartLighthouseAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await TurnOnAsync();
                    await Task.Delay(5000, token);
                    await TurnOffAsync();
                    await Task.Delay(5000, token);
                }
            }, token);

            return Task.CompletedTask;
        }

        public Task StopLighthouseAsync()
        {
            _cts?.Cancel();
            return TurnOffAsync();
        }

        public Task StartStrobeAsync(int intervalMs)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await TurnOnAsync();
                    await Task.Delay(intervalMs, token);
                    await TurnOffAsync();
                    await Task.Delay(intervalMs, token);
                }
            }, token);

            return Task.CompletedTask;
        }

        public Task StopStrobeAsync()
        {
            _cts?.Cancel();
            return TurnOffAsync();
        }

        public Task StartSOSAsync()
        {
            _cts?.Cancel();
            return StartMorseAsync("... --- ...");
        }

        public Task StopSOSAsync()
        {
            _cts?.Cancel();
            return TurnOffAsync();
        }

        public Task StartPoliceAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await TurnOnAsync();
                    await Task.Delay(500, token);
                    await TurnOffAsync();
                    await Task.Delay(500, token);
                }
            }, token);

            return Task.CompletedTask;
        }

        public Task StopPoliceAsync()
        {
            _cts?.Cancel();
            return TurnOffAsync();
        }

        public Task StartMorseAsync(string morse)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _ = Task.Run(async () =>
            {
                const int unit = 200;

                foreach (char c in morse)
                {
                    if (token.IsCancellationRequested) break;

                    if (c == '.')
                    {
                        await TurnOnAsync();
                        await Task.Delay(unit, token);
                        await TurnOffAsync();
                        await Task.Delay(unit, token);
                    }
                    else if (c == '-')
                    {
                        await TurnOnAsync();
                        await Task.Delay(unit * 3, token);
                        await TurnOffAsync();
                        await Task.Delay(unit, token);
                    }
                    else
                    {
                        await Task.Delay(unit * 3, token);
                    }
                }
            }, token);

            return Task.CompletedTask;
        }
    }
}
