using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware;
using Android.OS;
using AndroidX.Core.App;

using Microsoft.Maui.Storage;
using System.Text.Json;

namespace MAUI_Nonsense_App.Platforms.Android.Services.StepCounter
{
    [Service(ForegroundServiceType = ForegroundService.TypeSpecialUse)]
    public class StepCounterForegroundService : Service, ISensorEventListener
    {
        private SensorManager _sensorManager;
        private Sensor _stepSensor;

        public override void OnCreate()
        {
            base.OnCreate();

            _sensorManager = (SensorManager)GetSystemService(Context.SensorService);
            _stepSensor = _sensorManager?.GetDefaultSensor(SensorType.StepCounter);

            CreateNotificationChannel();

            var notification = new NotificationCompat.Builder(this, "step_counter_channel")
                .SetContentTitle("Step Counter")
                .SetContentText("Counting steps in background")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetOngoing(true)
                .Build();

            StartForeground(101, notification);

            if (_stepSensor != null)
                _sensorManager.RegisterListener(this, _stepSensor, SensorDelay.Ui);



        }

        public override void OnDestroy()
        {
            _sensorManager?.UnregisterListener(this, _stepSensor);
            base.OnDestroy();
        }

        public override IBinder OnBind(Intent intent) => null;

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent e)
        {
            int sensorValue = (int)e.Values[0];

            // Get today's date
            string today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            string lastDate = Preferences.Get("LastStepDate", today);

            // First ever value
            if (!Preferences.ContainsKey("FirstEverStepSensorValue"))
            {
                Preferences.Set("FirstEverStepSensorValue", sensorValue);
                Preferences.Set("LastStepDate", today);
                Preferences.Set("MidnightStepSensorValue", sensorValue);
            }

            int firstEver = Preferences.Get("FirstEverStepSensorValue", sensorValue);
            int midnight = Preferences.Get("MidnightStepSensorValue", sensorValue);

            // If day has changed, reset midnight baseline
            if (lastDate != today)
            {
                Preferences.Set("LastStepDate", today);
                Preferences.Set("MidnightStepSensorValue", sensorValue);
                midnight = sensorValue;
            }

            int totalSteps = sensorValue - firstEver;
            int dailySteps = sensorValue - midnight;

            Preferences.Set("AccumulatedSteps", totalSteps);
            Preferences.Set("DailySteps", dailySteps);

            UpdateStepHistory(today, dailySteps);




        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    "step_counter_channel",
                    "Step Counter Channel",
                    NotificationImportance.Low);

                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        private void UpdateStepHistory(string date, int stepsToday)
        {
            var json = Preferences.Get("StepHistory", "{}");
            var history = JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                          ?? new Dictionary<string, int>();

            history[date] = stepsToday;

            var updatedJson = JsonSerializer.Serialize(history);
            Preferences.Set("StepHistory", updatedJson);
        }

        public Dictionary<string, int> GetStepHistory()
        {
            var json = Preferences.Get("StepHistory", "{}");
            var history = JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                          ?? new Dictionary<string, int>();

            return history;
        }
    }
}