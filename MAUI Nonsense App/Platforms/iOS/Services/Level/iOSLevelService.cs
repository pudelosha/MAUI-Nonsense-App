using CoreMotion;
using Foundation;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Platforms.iOS.Services.Level;

public class iOSLevelService : ILevelService
{
    private readonly CMMotionManager _motionManager;
    private readonly NSOperationQueue _queue;

    public event EventHandler<LevelReading>? ReadingChanged;

    public iOSLevelService()
    {
        _motionManager = new CMMotionManager();
        _queue = NSOperationQueue.CurrentQueue ?? new NSOperationQueue();
    }

    public async Task StartAsync()
    {
        if (!_motionManager.DeviceMotionAvailable)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ Device motion not available.");
            return;
        }

        if (_motionManager.DeviceMotionActive)
            return;

        await Task.Run(() =>
        {
            _motionManager.DeviceMotionUpdateInterval = 0.1; // 10 Hz

            _motionManager.StartDeviceMotionUpdates(_queue, (data, error) =>
            {
                if (data == null || error != null)
                {
                    if (error != null)
                        System.Diagnostics.Debug.WriteLine($"Device motion error: {error.LocalizedDescription}");
                    return;
                }

                // Pitch and roll in radians
                var attitude = data.Attitude;

                double pitch = attitude.Pitch * 180.0 / Math.PI;
                double roll = attitude.Roll * 180.0 / Math.PI;

                ReadingChanged?.Invoke(this, new LevelReading(pitch, roll));
            });
        });
    }

    public async Task StopAsync()
    {
        await Task.Run(() =>
        {
            if (_motionManager.DeviceMotionActive)
            {
                _motionManager.StopDeviceMotionUpdates();
            }
        });
    }
}
