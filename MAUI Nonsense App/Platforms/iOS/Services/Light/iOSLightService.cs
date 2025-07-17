using System;
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
                        .DevicesWithMediaType("vide")  // kept exactly as your original
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

            NSError? error = null;
            if (_device.LockForConfiguration(out error))
            {
                _device.TorchMode = AVCaptureTorchMode.On;
                _device.UnlockForConfiguration();
                IsOn = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Failed to lock device: {error?.LocalizedDescription}");
            }
        }

        public async Task TurnOffAsync()
        {
            if (_device == null || !_device.TorchAvailable) return;

            NSError? error = null;
            if (_device.LockForConfiguration(out error))
            {
                _device.TorchMode = AVCaptureTorchMode.Off;
                _device.UnlockForConfiguration();
                IsOn = false;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Failed to lock device: {error?.LocalizedDescription}");
            }
        }

        public Task SetBrightnessAsync(double strength)
        {
            if (_device == null || !_device.HasTorch) return Task.CompletedTask;

            NSError? error = null;
            if (_device.LockForConfiguration(out error))
            {
                float level = (float)Math.Clamp(strength, 0.01, 1.0);
                NSError? levelError = null;
                _device.SetTorchModeLevel(level, out levelError);
                if (levelError != null)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Failed to set torch level: {levelError.LocalizedDescription}");
                }
                _device.UnlockForConfiguration();
            }

            return Task.CompletedTask;
        }

        public Task StartLighthouseAsync()
        {
            StartLoop(async token =>
            {
                while (!token.IsCancellationRequested)
                {
                    await TurnOnAsync();
                    await Task.Delay(5000, token);
                    await TurnOffAsync();
                    await Task.Delay(5000, token);
                }
            });
            return Task.CompletedTask;
        }

        public async Task StopLighthouseAsync()
        {
            StopCurrentLoop();
            await TurnOffAsync();
        }

        public Task StartStrobeAsync(int intervalMs)
        {
            StartLoop(async token =>
            {
                while (!token.IsCancellationRequested)
                {
                    await TurnOnAsync();
                    await Task.Delay(intervalMs, token);
                    await TurnOffAsync();
                    await Task.Delay(intervalMs, token);
                }
            });
            return Task.CompletedTask;
        }

        public async Task StopStrobeAsync()
        {
            StopCurrentLoop();
            await TurnOffAsync();
        }

        public Task StartSOSAsync()
        {
            return StartMorseAsync("... --- ...");
        }

        public async Task StopSOSAsync()
        {
            StopCurrentLoop();
            await TurnOffAsync();
        }

        public Task StartPoliceAsync()
        {
            StartLoop(async token =>
            {
                while (!token.IsCancellationRequested)
                {
                    await TurnOnAsync();
                    await Task.Delay(500, token);
                    await TurnOffAsync();
                    await Task.Delay(500, token);
                }
            });
            return Task.CompletedTask;
        }

        public async Task StopPoliceAsync()
        {
            StopCurrentLoop();
            await TurnOffAsync();
        }

        public Task StartMorseAsync(string morse)
        {
            StartLoop(async token =>
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
            });
            return Task.CompletedTask;
        }

        public async Task StopMorseAsync()
        {
            StopCurrentLoop();
            await TurnOffAsync();
        }

        private void StartLoop(Func<CancellationToken, Task> loopBody)
        {
            StopCurrentLoop();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _ = Task.Run(() => loopBody(token), token);
        }

        private void StopCurrentLoop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
    }
}
