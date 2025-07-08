using AApp = Android.App.Application;
using Android.Content;
using Android.Hardware;

namespace MAUI_Nonsense_App.Services.Android
{
    public class AndroidStepCounterService : Java.Lang.Object, ISensorEventListener, IStepCounterService
    {
        private SensorManager _sensorManager;
        private Sensor _stepSensor;
        private int _initialSteps;
        private const string InitialStepsKey = "InitialSteps";

        public int TotalSteps { get; private set; }
        public int Last24HoursSteps => TotalSteps; // placeholder — real 24h tracking needs storage

        public event EventHandler StepsUpdated;

        public AndroidStepCounterService()
        {
            _sensorManager = (SensorManager)AApp.Context.GetSystemService(Context.SensorService);
            _stepSensor = _sensorManager?.GetDefaultSensor(SensorType.StepCounter);

            _initialSteps = Preferences.Get(InitialStepsKey, -1);
        }

        public Task StartAsync()
        {
            if (_stepSensor != null)
                _sensorManager.RegisterListener(this, _stepSensor, SensorDelay.Ui);

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _sensorManager?.UnregisterListener(this, _stepSensor);
            return Task.CompletedTask;
        }

        public void OnSensorChanged(SensorEvent e)
        {
            if (_initialSteps == -1)
            {
                _initialSteps = (int)e.Values[0];
                Preferences.Set(InitialStepsKey, _initialSteps);
            }

            TotalSteps = (int)e.Values[0] - _initialSteps;

            StepsUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) { }
    }
}
