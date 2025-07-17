using Android.Content;
using Android.Hardware;
using Android.Runtime;
using MAUI_Nonsense_App.Services;
using AApp = Android.App.Application;

namespace MAUI_Nonsense_App.Platforms.Android.Services.Level;

public class AndroidLevelService : Java.Lang.Object, ILevelService, ISensorEventListener
{
    private readonly SensorManager _sensorManager;
    private readonly Sensor _accelerometer;
    private readonly Sensor _magnetometer;

    private float[] _gravity = new float[3];
    private float[] _geomagnetic = new float[3];
    private readonly float[] _rotationMatrix = new float[9];
    private readonly float[] _orientation = new float[3];

    public event EventHandler<LevelReading>? ReadingChanged;

    private bool _running;

    public AndroidLevelService()
    {
        var context = AApp.Context ?? throw new InvalidOperationException("Application Context is null.");
        _sensorManager = (SensorManager)context.GetSystemService(Context.SensorService)!;

        _accelerometer = _sensorManager.GetDefaultSensor(SensorType.Accelerometer)!;
        _magnetometer = _sensorManager.GetDefaultSensor(SensorType.MagneticField)!;
    }

    public Task StartAsync()
    {
        if (_running) return Task.CompletedTask;

        SensorDelay delay = SensorDelay.Game; // always use high accuracy

        _sensorManager.RegisterListener(this, _accelerometer, delay);
        _sensorManager.RegisterListener(this, _magnetometer, delay);

        _running = true;

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (!_running) return Task.CompletedTask;

        _sensorManager.UnregisterListener(this);
        _running = false;

        return Task.CompletedTask;
    }

    public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
    {
        // no-op
    }

    public void OnSensorChanged(SensorEvent? e)
    {
        if (e == null) return;

        if (e.Sensor.Type == SensorType.Accelerometer)
        {
            Array.Copy(e.Values.ToArray(), _gravity, 3);
        }
        else if (e.Sensor.Type == SensorType.MagneticField)
        {
            Array.Copy(e.Values.ToArray(), _geomagnetic, 3);
        }

        if (_gravity != null && _geomagnetic != null)
        {
            bool success = SensorManager.GetRotationMatrix(_rotationMatrix, null, _gravity, _geomagnetic);
            if (success)
            {
                SensorManager.GetOrientation(_rotationMatrix, _orientation);

                double pitch = _orientation[1] * (180.0 / Math.PI); // x
                double roll = _orientation[2] * (180.0 / Math.PI);  // y

                ReadingChanged?.Invoke(this, new LevelReading(pitch, roll));
            }
        }
    }
}
