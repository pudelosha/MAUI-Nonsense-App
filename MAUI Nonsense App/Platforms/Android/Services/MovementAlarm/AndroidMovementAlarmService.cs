using Android.Content;
using Android.Hardware;
using Android.Media;
using Android.OS;
using MAUI_Nonsense_App.Platforms.Android.Helpers;
using MAUI_Nonsense_App.Services;
using Microsoft.Maui; // for Device.StartTimer
using AApp = Android.App.Application;

namespace MAUI_Nonsense_App.Platforms.Android.Services.MovementAlarm
{
    public class AndroidMovementAlarmService : Java.Lang.Object, IMovementAlarmService, ISensorEventListener
    {
        private readonly SensorManager _sensorManager;
        private readonly Sensor _accelerometer;

        private bool _armed;
        private int _armingDelaySeconds = 10;
        private int _sensitivity = 2;

        private BroadcastReceiverWrapper? _receiver;
        private MediaPlayer? _mediaPlayer;
        private bool _alarmActive = false;
        private CancellationTokenSource? _alarmCts;

        public event EventHandler? MovementDetected;

        public AndroidMovementAlarmService()
        {
            var context = AApp.Context!;
            _sensorManager = (SensorManager)context.GetSystemService(Context.SensorService)!;
            _accelerometer = _sensorManager.GetDefaultSensor(SensorType.Accelerometer)!;
        }

        public void Configure(int armingDelaySeconds, int sensitivity)
        {
            _armingDelaySeconds = armingDelaySeconds;
            _sensitivity = sensitivity;
        }

        public Task StartAsync()
        {
            var context = AApp.Context!;

            // Start foreground service
            var intent = new Intent(context, typeof(MovementAlarmForegroundService));
            intent.PutExtra("armingDelay", _armingDelaySeconds);
            intent.PutExtra("sensitivity", _sensitivity);
            context.StartForegroundService(intent);

            // Register broadcast receiver
            _receiver = new BroadcastReceiverWrapper(() => MovementDetected?.Invoke(this, EventArgs.Empty));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                context.RegisterReceiver(
                    _receiver,
                    new IntentFilter("MAUI_NONSENSE_MOVEMENT_DETECTED"),
                    ReceiverFlags.NotExported);
            }
            else
            {
                context.RegisterReceiver(
                    _receiver,
                    new IntentFilter("MAUI_NONSENSE_MOVEMENT_DETECTED"));
            }

            // Register sensor listener
            _sensorManager.RegisterListener(this, _accelerometer, SensorDelay.Game);

            _armed = false;
            System.Diagnostics.Debug.WriteLine($"Movement alarm: arming in {_armingDelaySeconds} seconds…");

            Device.StartTimer(TimeSpan.FromSeconds(_armingDelaySeconds), () =>
            {
                _armed = true;
                System.Diagnostics.Debug.WriteLine("Movement alarm: ARMED.");
                return false; // don’t repeat
            });

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            var context = AApp.Context!;
            context.StopService(new Intent(context, typeof(MovementAlarmForegroundService)));

            if (_receiver != null)
            {
                context.UnregisterReceiver(_receiver);
                _receiver = null;
            }

            // Unregister sensor listener
            _sensorManager.UnregisterListener(this);

            // Stop sound
            StopSound();

            _armed = false;

            System.Diagnostics.Debug.WriteLine("Movement alarm: DISARMED.");

            return Task.CompletedTask;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e == null || !_armed) return;

            float x = e.Values[0];
            float y = e.Values[1];
            float z = e.Values[2];

            double magnitude = Math.Sqrt(x * x + y * y + z * z) - SensorManager.GravityEarth;

            double threshold = _sensitivity switch
            {
                1 => 1.5,
                2 => 0.75,
                3 => 0.25,
                _ => 0.75
            };

            System.Diagnostics.Debug.WriteLine($"magnitude={magnitude:F2}, threshold={threshold}");

            if (Math.Abs(magnitude) > threshold)
            {
                System.Diagnostics.Debug.WriteLine($"Movement detected! magnitude={magnitude:F2} > threshold={threshold}");
                MovementDetectedBroadcast();
                StartAlarmForDuration(10_000);
            }
        }

        private void MovementDetectedBroadcast()
        {
            var context = AApp.Context!;
            var intent = new Intent("MAUI_NONSENSE_MOVEMENT_DETECTED");
            context.SendBroadcast(intent);
        }

        private void StartAlarmForDuration(int milliseconds)
        {
            if (!_alarmActive)
            {
                PlaySound();
                _alarmActive = true;
            }

            // Cancel previous timer
            _alarmCts?.Cancel();
            _alarmCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(milliseconds, _alarmCts.Token);
                    StopSound();
                }
                catch (TaskCanceledException)
                {
                    // Another motion extended the alarm
                }
            });
        }

        private void PlaySound()
        {
            StopSound(); // stop any existing

            var context = AApp.Context!;
            var alarmUri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm);

            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.SetDataSource(context, alarmUri);
            _mediaPlayer.Looping = true;
            _mediaPlayer.Prepare();
            _mediaPlayer.Start();

            System.Diagnostics.Debug.WriteLine("Alarm sound started (looping).");
        }

        private void StopSound()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Release();
                _mediaPlayer.Dispose();
                _mediaPlayer = null;

                System.Diagnostics.Debug.WriteLine("Alarm sound stopped.");
            }

            _alarmActive = false;
            _alarmCts?.Cancel();
            _alarmCts = null;
        }
    }
}
