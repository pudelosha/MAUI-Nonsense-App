using System;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Microsoft.Maui.Storage;
using AApp = Android.App.Application;

namespace MAUI_Nonsense_App.Services.Android
{
    public class AndroidStepCounterService : IStepCounterService
    {
        private readonly Context _context;
        private readonly Handler _handler;
        private const int PollIntervalMs = 2000;

        public int TotalSteps => Preferences.Get("TotalSteps", 0);
        public int Last24HoursSteps => TotalSteps; // Placeholder

        public event EventHandler StepsUpdated;

        public AndroidStepCounterService()
        {
            _context = AApp.Context;
            _handler = new Handler();
        }

        public Task StartAsync()
        {
            StartForegroundService();
            StartPolling();
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            StopForegroundService();
            _handler.RemoveCallbacksAndMessages(null);
            return Task.CompletedTask;
        }

        private void StartForegroundService()
        {
            var intent = new Intent(_context, typeof(MAUI_Nonsense_App.Platforms.Android.Services.StepCounterForegroundService));

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                _context.StartForegroundService(intent);
            }
            else
            {
                _context.StartService(intent);
            }
        }

        private void StopForegroundService()
        {
            var intent = new Intent(_context, typeof(MAUI_Nonsense_App.Platforms.Android.Services.StepCounterForegroundService));
            _context.StopService(intent);
        }

        private void StartPolling()
        {
            _handler.PostDelayed(() =>
            {
                StepsUpdated?.Invoke(this, EventArgs.Empty);
                StartPolling();
            }, PollIntervalMs);
        }
    }
}
