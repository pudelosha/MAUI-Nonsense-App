using Android.Content;
using Android.Hardware;
using System.Text.Json;
using AApp = Android.App.Application;

namespace MAUI_Nonsense_App.Platforms.Android.Services.StepCounter
{
    public class AndroidStepCounterService : Java.Lang.Object, IStepCounterService, ISensorEventListener
    {
        private readonly SensorManager _sensorManager;
        private readonly Sensor? _stepSensor;
        private int _lastSensorValue;

        public int RawSensorValue => _lastSensorValue;

        public AndroidStepCounterService()
        {
            _lastSensorValue = Preferences.Get("LastSensorValue", 0);

            _sensorManager = (SensorManager)AApp.Context.GetSystemService(Context.SensorService);
            _stepSensor = _sensorManager?.GetDefaultSensor(SensorType.StepCounter);

            if (_stepSensor != null)
            {
                _sensorManager.RegisterListener(this, _stepSensor, SensorDelay.Ui);
            }
        }

        public int TotalSteps => GetAdjustedTotalSteps();

        public int Last24HoursSteps
        {
            get
            {
                int midnight = Preferences.Get("MidnightStepSensorValue", GetAdjustedTotalSteps());
                return Math.Max(0, GetAdjustedTotalSteps() - midnight);
            }
        }

        public Dictionary<string, int> StepHistory
        {
            get
            {
                var history = LoadStepHistory();
                var today = DateTime.Now.ToString("yyyy-MM-dd"); // Local date

                // Always show live value for today
                history[today] = GetAdjustedTotalSteps();

                return history;
            }
        }

        public void OnSensorChanged(SensorEvent e)
        {
            if (e?.Sensor?.Type == SensorType.StepCounter)
            {
                _lastSensorValue = (int)e.Values[0];
                Preferences.Set("LastSensorValue", _lastSensorValue);

                if (!Preferences.ContainsKey("BootBaseStepValue"))
                {
                    Preferences.Set("BootBaseStepValue", _lastSensorValue);
                }

                SaveDailySnapshotIfNeeded(GetAdjustedTotalSteps());
            }
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
            // Not needed
        }

        private int GetAdjustedTotalSteps()
        {
            int bootBase = Preferences.Get("BootBaseStepValue", -1);

            if (bootBase < 0 || _lastSensorValue < bootBase)
            {
                // Likely reboot occurred and no valid base saved
                Preferences.Set("BootBaseStepValue", _lastSensorValue);
                return 0; // This is now the new baseline
            }

            return Math.Max(0, _lastSensorValue - bootBase);
        }


        public void SaveDailySnapshotIfNeeded(int adjustedSteps)
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var lastSavedDate = Preferences.Get("LastStepDate", "");

            if (today != lastSavedDate)
            {
                var history = LoadStepHistory();

                // Save previous day step count
                if (!string.IsNullOrEmpty(lastSavedDate))
                {
                    int previousMidnight = Preferences.Get("MidnightStepSensorValue", 0);
                    history[lastSavedDate] = previousMidnight;
                }

                Preferences.Set("MidnightStepSensorValue", adjustedSteps); // new day's start value
                Preferences.Set("LastStepDate", today);
                Preferences.Set("StepHistory", JsonSerializer.Serialize(history));
            }
        }

        private Dictionary<string, int> LoadStepHistory()
        {
            var json = Preferences.Get("StepHistory", "{}");
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                   ?? new Dictionary<string, int>();
        }

        public Task StartAsync() => Task.CompletedTask;

        public Task StopAsync()
        {
            _sensorManager?.UnregisterListener(this);
            return Task.CompletedTask;
        }

        public void ResetAll()
        {
            Preferences.Remove("BootBaseStepValue");
            Preferences.Remove("LastSensorValue");
            Preferences.Remove("MidnightStepSensorValue");
            Preferences.Remove("LastStepDate");
            Preferences.Set("StepHistory", "{}");
        }

        public int GetRawSensorValue() => _lastSensorValue;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _sensorManager?.UnregisterListener(this, _stepSensor);
        }
    }
}
