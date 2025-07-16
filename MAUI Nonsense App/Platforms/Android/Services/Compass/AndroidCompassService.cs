using Android.Content;
using Android.Hardware;
using Android.Runtime;
using MAUI_Nonsense_App.Services;
using AApp = Android.App.Application;

namespace MAUI_Nonsense_App.Platforms.Android.Services.Compass
{
    public class AndroidCompassService : Java.Lang.Object, ICompassService, ISensorEventListener
    {
        private readonly SensorManager _sensorManager;
        private readonly Sensor _sensor;

        public event EventHandler<double>? HeadingChanged;
        public event EventHandler<CompassAccuracy>? AccuracyChanged;

        private DateTime _compassStartTime;

        public AndroidCompassService()
        {
            var context = AApp.Context ?? throw new InvalidOperationException("Application Context is null.");
            _sensorManager = (SensorManager)context.GetSystemService(Context.SensorService)!;
            _sensor = _sensorManager.GetDefaultSensor(SensorType.Orientation);
        }

        public void Start(bool highAccuracy = true)
        {
            // You can adjust SensorDelay depending on highAccuracy
            var delay = highAccuracy ? SensorDelay.Game : SensorDelay.Ui;
            _compassStartTime = DateTime.UtcNow;
            _sensorManager.RegisterListener(this, _sensor, delay);
        }

        public void Stop()
        {
            _sensorManager.UnregisterListener(this, _sensor);
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {
            var mappedAccuracy = accuracy switch
            {
                SensorStatus.Unreliable => CompassAccuracy.Unreliable,
                SensorStatus.AccuracyLow => CompassAccuracy.Low,
                SensorStatus.AccuracyMedium => CompassAccuracy.Medium,
                SensorStatus.AccuracyHigh => CompassAccuracy.High,
                _ => CompassAccuracy.Unreliable
            };

            AccuracyChanged?.Invoke(this, mappedAccuracy);
        }

        public void OnSensorChanged(SensorEvent e)
        {
            double azimuth = e.Values[0];

            if (DateTime.UtcNow - _compassStartTime < TimeSpan.FromSeconds(2))
            {
                // ignore unstable initial readings
                return;
            }

            HeadingChanged?.Invoke(this, azimuth);
        }
    }
}
