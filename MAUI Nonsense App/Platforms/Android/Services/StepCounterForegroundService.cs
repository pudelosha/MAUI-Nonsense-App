using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Maui.Storage;

namespace MAUI_Nonsense_App.Platforms.Android.Services
{
    [Service(ForegroundServiceType = ForegroundService.TypeSpecialUse)]
    public class StepCounterForegroundService : Service, ISensorEventListener
    {
        private SensorManager _sensorManager;
        private Sensor _stepSensor;
        private int _initialSteps;

        public override void OnCreate()
        {
            base.OnCreate();

            _sensorManager = (SensorManager)GetSystemService(Context.SensorService);
            _stepSensor = _sensorManager?.GetDefaultSensor(SensorType.StepCounter);

            _initialSteps = Preferences.Get("InitialSteps", -1);

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
            if (_initialSteps == -1)
            {
                _initialSteps = (int)e.Values[0];
                Preferences.Set("InitialSteps", _initialSteps);
            }

            int totalSteps = (int)e.Values[0] - _initialSteps;
            Preferences.Set("TotalSteps", totalSteps);
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
    }
}
