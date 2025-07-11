using Android.Content;
using Android.Hardware.Camera2;
using Android.Runtime;
using MAUI_Nonsense_App.Services;
using AApp = Android.App.Application;

namespace MAUI_Nonsense_App.Platforms.Android.Services.Light
{
    public class AndroidLightService : ILightService
    {
        private readonly CameraManager _cameraManager;
        private readonly string? _cameraId;
        private bool _isOn;
        private CancellationTokenSource? _cts;

        public AndroidLightService()
        {
            var context = AApp.Context ?? throw new InvalidOperationException("Application Context is null.");
            _cameraManager = (CameraManager)context.GetSystemService(Context.CameraService)!;

            _cameraId = _cameraManager
                .GetCameraIdList()
                .FirstOrDefault(id =>
                    _cameraManager
                        .GetCameraCharacteristics(id)
                        .Get(CameraCharacteristics.FlashInfoAvailable)?
                        .JavaCast<Java.Lang.Boolean>()?.BooleanValue() == true);

            if (_cameraId == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No camera with flash available.");
            }
        }

        public bool IsOn => _isOn;

        public Task<bool> IsSupportedAsync() => Task.FromResult(_cameraId != null);

        public async Task TurnOnAsync()
        {
            if (_cameraId == null) return;
            _cameraManager.SetTorchMode(_cameraId, true);
            _isOn = true;
        }

        public async Task TurnOffAsync()
        {
            if (_cameraId == null) return;
            _cameraManager.SetTorchMode(_cameraId, false);
            _isOn = false;
        }

        public Task SetBrightnessAsync(double strength) => Task.CompletedTask;

        public async Task StartLighthouseAsync()
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
            });
        }

        public Task StopLighthouseAsync()
        {
            _cts?.Cancel();
            return TurnOffAsync();
        }

        public async Task StartPoliceAsync()
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
            });
        }

        public Task StopPoliceAsync()
        {
            _cts?.Cancel();
            return TurnOffAsync();
        }


        public async Task StartStrobeAsync(int intervalMs)
        {
            StopCurrentLoop();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await TurnOnAsync();
                        await Task.Delay(intervalMs, token);
                        await TurnOffAsync();
                        await Task.Delay(intervalMs, token);
                    }
                }
                catch (TaskCanceledException) { }
                finally
                {
                    await TurnOffAsync();
                }
            });
        }

        public async Task StopStrobeAsync()
        {
            StopCurrentLoop();
            await TurnOffAsync();
        }

        public async Task StartSOSAsync()
        {
            StopCurrentLoop();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await PlayMorseCodeAsync(token);
                        await Task.Delay(1000, token);
                    }
                }
                catch (TaskCanceledException) { }
                finally
                {
                    await TurnOffAsync();
                }
            });
        }

        public async Task StopSOSAsync()
        {
            StopCurrentLoop();
            await TurnOffAsync();
        }

        private void StopCurrentLoop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private async Task PlayMorseCodeAsync(CancellationToken token)
        {
            const string morse = "... --- ...";
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
        }

        public async Task StartMorseAsync(string morse)
        {
            _cts?.Cancel(); // cancel any previous job
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
                        await Task.Delay(unit * 3, token); // space
                    }
                }
            });
        }
    }
}
