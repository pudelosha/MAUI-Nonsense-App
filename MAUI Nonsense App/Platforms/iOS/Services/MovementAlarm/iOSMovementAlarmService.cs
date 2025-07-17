using CoreMotion;
using Foundation;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Platforms.iOS.Services.MovementAlarm
{
    public class iOSMovementAlarmService : IMovementAlarmService
    {
        private readonly CMMotionManager _motionManager;
        private NSTimer? _armingTimer;
        private bool _armed;
        private int _armingDelaySeconds = 10;
        private int _sensitivity = 2;

        public event EventHandler? MovementDetected;

        public iOSMovementAlarmService()
        {
            _motionManager = new CMMotionManager();
        }

        public void Configure(int armingDelaySeconds, int sensitivity)
        {
            _armingDelaySeconds = armingDelaySeconds;
            _sensitivity = sensitivity;
        }

        public Task StartAsync()
        {
            _armed = false;

            _motionManager.StartAccelerometerUpdates(NSOperationQueue.CurrentQueue, (data, error) =>
            {
                if (!_armed || data == null) return;

                double x = data.Acceleration.X;
                double y = data.Acceleration.Y;
                double z = data.Acceleration.Z;

                double magnitude = Math.Sqrt(x * x + y * y + z * z) - 1.0;

                double threshold = _sensitivity switch
                {
                    1 => 0.5,   // low
                    2 => 0.2,   // medium
                    3 => 0.05,  // high
                    _ => 0.2
                };

                if (Math.Abs(magnitude) > threshold)
                {
                    MovementDetected?.Invoke(this, EventArgs.Empty);
                }
            });

            _armingTimer = NSTimer.CreateScheduledTimer(
                TimeSpan.FromSeconds(_armingDelaySeconds),
                timer =>
                {
                    _armed = true;
                });

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _motionManager.StopAccelerometerUpdates();
            _armingTimer?.Invalidate();
            _armingTimer = null;
            _armed = false;
            return Task.CompletedTask;
        }
    }
}

