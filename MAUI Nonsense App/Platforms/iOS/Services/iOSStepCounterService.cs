using CoreMotion;
using Foundation;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Platforms.iOS.Services
{
    public class iOSStepCounterService : IStepCounterService
    {
        private readonly CMPedometer _pedometer = new CMPedometer();
        private int _initialSteps;
        private const string InitialStepsKey = "InitialSteps";

        public int TotalSteps { get; private set; }
        public int Last24HoursSteps { get; private set; }

        public event EventHandler StepsUpdated;

        public async Task StartAsync()
        {
            if (!CMPedometer.IsStepCountingAvailable)
                return;

            var now = NSDate.Now;

            var from = now.AddSeconds(-24 * 3600);
            var summary = await _pedometer.QueryPedometerDataAsync(from, now);
            Last24HoursSteps = summary?.NumberOfSteps?.Int32Value ?? 0;

            _initialSteps = Preferences.Get(InitialStepsKey, -1);

            _pedometer.StartPedometerUpdates(now, (data, error) =>
            {
                if (data != null)
                {
                    int currentSteps = data.NumberOfSteps?.Int32Value ?? 0;

                    if (_initialSteps == -1)
                    {
                        _initialSteps = currentSteps;
                        Preferences.Set(InitialStepsKey, _initialSteps);
                    }

                    TotalSteps = currentSteps - _initialSteps;
                    StepsUpdated?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        public Task StopAsync()
        {
            _pedometer.StopPedometerUpdates();
            return Task.CompletedTask;
        }
    }
}
