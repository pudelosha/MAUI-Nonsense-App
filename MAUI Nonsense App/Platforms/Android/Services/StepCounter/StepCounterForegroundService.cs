using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware;
using Android.OS;
using Android.Util;
using AndroidX.Core.App;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Text.Json;

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
            _isUsingStepCounter = _stepSensor != null;

            if (_stepSensor == null)
            {
                _stepSensor = _sensorManager?.GetDefaultSensor(SensorType.StepDetector);
                Log.Warn("StepCounter", "No StepCounter; using StepDetector.");
            }

            CreateNotificationChannel();
            var ntf = new NotificationCompat.Builder(this, "step_counter_channel")
                .SetContentTitle("Step Counter")
                .SetContentText("Counting steps in background")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetOngoing(true)
                .Build();
            StartForeground(101, ntf);

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
            var nowLocal = DateTime.Now;
            var todayKey = nowLocal.Date.ToString("yyyy-MM-dd");
            var lastDate = Preferences.Get("LastStepDate", todayKey);
            var nowMs = Java.Lang.JavaSystem.CurrentTimeMillis();
            long lastTickMs = Preferences.Get("LastStepUnixMs", 0L);

            int deltaSteps = 0;

            if (_isUsingStepCounter)
            {
                int currentValue = (int)e.Values[0]; // cumulative since boot

                bool hasBaseline = Preferences.ContainsKey("LastSensorReading");
                int lastValue = hasBaseline ? Preferences.Get("LastSensorReading", currentValue)
                                            : currentValue;

                // First tick after install/redeploy: establish baselines only
                if (!hasBaseline)
                {
                    Preferences.Set("LastSensorReading", currentValue);
                    if (!Preferences.ContainsKey("MidnightStepSensorValue"))
                        Preferences.Set("MidnightStepSensorValue", currentValue);
                    Preferences.Set("LastStepDate", todayKey);
                    Preferences.Set("LastStepUnixMs", nowMs);
                    AndroidStepCounterService.Instance?.RaiseStepsUpdated();
                    return;
                }

                // Roll midnight if date changed (extra safety if alarm missed)
                if (lastDate != todayKey || !Preferences.ContainsKey("MidnightStepSensorValue"))
                {
                    Preferences.Set("MidnightStepSensorValue", currentValue);
                    Preferences.Set("LastStepDate", todayKey);
                    Preferences.Set("RebootDailyOffset", 0);
                    Preferences.Set("ActiveSecondsToday", 0L);
                    Preferences.Set("DailySteps", 0);
                    lastTickMs = 0L;
                }

                // Detect reboot / sensor reset (counter dropped)
                if (currentValue < lastValue)
                {
                    int prevDaily = Preferences.Get("DailySteps", 0);
                    int ro = Preferences.Get("RebootDailyOffset", 0);
                    long newOffset = (long)ro + Math.Max(0, prevDaily);
                    Preferences.Set("RebootDailyOffset", newOffset > int.MaxValue ? int.MaxValue : (int)newOffset);

                    Preferences.Set("MidnightStepSensorValue", 0);
                    deltaSteps = 0;
                }
                else
                {
                    deltaSteps = Math.Max(0, currentValue - lastValue);
                }

                Preferences.Set("LastSensorReading", currentValue);

                int midnight = Preferences.Get("MidnightStepSensorValue", currentValue);
                int rebootOffset = Preferences.Get("RebootDailyOffset", 0);
                long rawDaily = (long)rebootOffset + Math.Max(0, currentValue - midnight);
                int dailySteps = rawDaily < 0 ? 0 : (rawDaily > int.MaxValue ? int.MaxValue : (int)rawDaily);

                // Update accumulators
                int runningTotal = Preferences.Get("RunningTotalSteps", 0) + deltaSteps;
                Preferences.Set("RunningTotalSteps", runningTotal);
                Preferences.Set("AccumulatedSteps", runningTotal);
                Preferences.Set("DailySteps", dailySteps);

                // History
                UpdateDailyHistory(todayKey, dailySteps);
                if (deltaSteps > 0) UpdateHourlyHistory(todayKey, nowLocal.Hour, deltaSteps);

                if (dailySteps != _lastNotifiedSteps)
                {
                    UpdateNotification(dailySteps);
                    _lastNotifiedSteps = dailySteps;
                }
            }
            else
            {
                // StepDetector (+1 per event)
                int dailySteps = Preferences.Get("DailySteps", 0);

                if (lastDate != todayKey)
                {
                    Preferences.Set("LastStepDate", todayKey);
                    Preferences.Set("RebootDailyOffset", 0);
                    Preferences.Set("ActiveSecondsToday", 0L);
                    Preferences.Set("DailySteps", 0);
                    lastTickMs = 0L;
                    dailySteps = 0;
                }

                dailySteps += 1;

                int runningTotal = Preferences.Get("RunningTotalSteps", 0) + 1;
                Preferences.Set("RunningTotalSteps", runningTotal);
                Preferences.Set("AccumulatedSteps", runningTotal);
                Preferences.Set("DailySteps", dailySteps);

                UpdateDailyHistory(todayKey, dailySteps);
                UpdateHourlyHistory(todayKey, nowLocal.Hour, 1);

                if (dailySteps != _lastNotifiedSteps)
                {
                    UpdateNotification(dailySteps);
                    _lastNotifiedSteps = dailySteps;
                }
            }

            // Active-time accumulation
            if (lastTickMs > 0)
            {
                var deltaSec = (nowMs - lastTickMs) / 1000;
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
                var nm = (NotificationManager)GetSystemService(NotificationService);
                nm.CreateNotificationChannel(channel);
            }
        }

        private void UpdateNotification(int steps)
        {
            var ntf = new NotificationCompat.Builder(this, "step_counter_channel")
                .SetContentTitle("Step Counter")
                .SetContentText($"Today's steps: {steps}")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetOngoing(true)
                .Build();
            NotificationManagerCompat.From(this).Notify(101, ntf);
        }

        // -------- storage helpers --------
        private void UpdateDailyHistory(string dayKey, int stepsToday)
        {
            var dailyJson = Preferences.Get("StepHistoryDaily",
                              Preferences.Get("StepHistory", "{}"));
            var daily = JsonSerializer.Deserialize<Dictionary<string, int>>(dailyJson)
                        ?? new Dictionary<string, int>();
            daily[dayKey] = stepsToday; // absolute per-day
            Preferences.Set("StepHistoryDaily", JsonSerializer.Serialize(daily));
        }

        private void UpdateHourlyHistory(string dayKey, int hour, int delta)
        {
            var hourlyJson = Preferences.Get("StepHistoryHourly", "{}");
            var hourly = JsonSerializer.Deserialize<Dictionary<string, int[]>>(hourlyJson)
                         ?? new Dictionary<string, int[]>();
            if (!hourly.TryGetValue(dayKey, out var arr) || arr == null || arr.Length != 24)
                arr = new int[24];

            long v = (long)arr[hour] + delta;
            arr[hour] = v > int.MaxValue ? int.MaxValue : (int)v;

            hourly[dayKey] = arr;
            Preferences.Set("StepHistoryHourly", JsonSerializer.Serialize(hourly));
        }
    }
}
