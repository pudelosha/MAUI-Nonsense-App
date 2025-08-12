using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Maui.Storage;
using System.Text.Json;
using Android.Util;
using System;
using System.Collections.Generic;

namespace MAUI_Nonsense_App.Platforms.Android.Services.StepCounter
{
    [Service(ForegroundServiceType = ForegroundService.TypeSpecialUse)]
    public class StepCounterForegroundService : Service, ISensorEventListener
    {
        private SensorManager _sensorManager;
        private Sensor _stepSensor;
        private bool _isUsingStepCounter = true;
        private int _lastNotifiedSteps = -1;

        public override void OnCreate()
        {
            base.OnCreate();
            Log.Info("StepCounter", "StepCounterForegroundService started");

            _sensorManager = (SensorManager)GetSystemService(Context.SensorService);
            _stepSensor = _sensorManager?.GetDefaultSensor(SensorType.StepCounter);
            _isUsingStepCounter = true;

            if (_stepSensor == null)
            {
                _stepSensor = _sensorManager?.GetDefaultSensor(SensorType.StepDetector);
                _isUsingStepCounter = false;
                Log.Warn("StepCounter", "StepCounter sensor not available. Using StepDetector instead.");
            }

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
            else
                Log.Error("StepCounter", "No step sensor available on this device.");
        }

        public override void OnDestroy()
        {
            Log.Info("StepCounter", "StepCounterForegroundService stopped");

            _sensorManager?.UnregisterListener(this, _stepSensor);
            base.OnDestroy();
        }

        public override IBinder OnBind(Intent intent) => null;

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent e)
        {
            Log.Info("StepCounter", $"Sensor value: {e.Values[0]}");

            // Timestamp for active-time accumulation
            var nowMs = Java.Lang.JavaSystem.CurrentTimeMillis();
            long lastTickMs = Preferences.Get("LastStepUnixMs", 0L);

            string today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            string lastDate = Preferences.Get("LastStepDate", today);

            if (_isUsingStepCounter)
            {
                int currentValue = (int)e.Values[0]; // steps since last boot
                int lastValue = Preferences.Get("LastSensorReading", currentValue);
                int delta = Math.Max(0, currentValue - lastValue);

                int runningTotal = Preferences.Get("RunningTotalSteps", 0);
                runningTotal += delta;

                Preferences.Set("RunningTotalSteps", runningTotal);
                Preferences.Set("LastSensorReading", currentValue);

                // If date changed or baseline missing, set baseline relative to current counter
                if (lastDate != today || !Preferences.ContainsKey("MidnightStepSensorValue"))
                {
                    Preferences.Set("MidnightStepSensorValue", currentValue);
                    Preferences.Set("LastStepDate", today);
                    Preferences.Set("RebootDailyOffset", 0);

                    // Reset active-time accumulation on day change
                    Preferences.Set("ActiveSecondsToday", 0L);
                    lastTickMs = 0L;
                }

                int midnight = Preferences.Get("MidnightStepSensorValue", currentValue);
                int rebootOffset = Preferences.Get("RebootDailyOffset", 0);

                long rawDaily = (long)rebootOffset + Math.Max(0, currentValue - midnight);
                int dailySteps = rawDaily < 0 ? 0 : (rawDaily > int.MaxValue ? int.MaxValue : (int)rawDaily);

                Preferences.Set("AccumulatedSteps", runningTotal);
                Preferences.Set("DailySteps", dailySteps);
                UpdateStepHistory(today, dailySteps);

                // Update the persistent notification
                if (dailySteps != _lastNotifiedSteps)
                {
                    UpdateNotification(dailySteps);
                    _lastNotifiedSteps = dailySteps;
                }
            }
            else
            {
                // StepDetector emits 1 per step; reboot doesn't reset the logic here,
                // but we still respect date changes and history.
                int runningTotal = Preferences.Get("RunningTotalSteps", 0) + 1;
                int dailySteps = Preferences.Get("DailySteps", 0);

                if (lastDate != today)
                {
                    Preferences.Set("LastStepDate", today);
                    Preferences.Set("RebootDailyOffset", 0);

                    // Reset active-time accumulation on day change
                    Preferences.Set("ActiveSecondsToday", 0L);
                    lastTickMs = 0L;

                    dailySteps = 0;
                }

                dailySteps += 1;

                Preferences.Set("RunningTotalSteps", runningTotal);
                Preferences.Set("AccumulatedSteps", runningTotal);
                Preferences.Set("DailySteps", dailySteps);
                UpdateStepHistory(today, dailySteps);

                if (dailySteps != _lastNotifiedSteps)
                {
                    UpdateNotification(dailySteps);
                    _lastNotifiedSteps = dailySteps;
                }
            }

            // ---- Active time accumulation (shared) ----
            if (lastTickMs > 0)
            {
                var deltaSec = (nowMs - lastTickMs) / 1000;
                // Treat short gaps as continuous walking
                if (deltaSec > 0 && deltaSec <= 10)
                {
                    long secs = Preferences.Get("ActiveSecondsToday", 0L);
                    secs += deltaSec;
                    Preferences.Set("ActiveSecondsToday", secs);
                }
            }
            Preferences.Set("LastStepUnixMs", nowMs);

            AndroidStepCounterService.Instance?.RaiseStepsUpdated();
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

        private void UpdateNotification(int steps)
        {
            var notification = new NotificationCompat.Builder(this, "step_counter_channel")
                .SetContentTitle("Step Counter")
                .SetContentText($"Today's steps: {steps}")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetOngoing(true)
                .Build();

            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(101, notification); // same ID = update
        }

        private void UpdateStepHistory(string date, int stepsToday)
        {
            var json = Preferences.Get("StepHistory", "{}");
            var history = JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                          ?? new Dictionary<string, int>();

            history[date] = stepsToday;
            Preferences.Set("StepHistory", JsonSerializer.Serialize(history));
        }
    }
}
