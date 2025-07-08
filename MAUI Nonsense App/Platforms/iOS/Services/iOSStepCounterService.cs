using CoreMotion;
using Foundation;

namespace MAUI_Nonsense_App.Services.iOS
{
    public class iOSStepCounterService : IStepCounterService
    {
        private readonly CMPedometer _pedometer = new CMPedometer();
        private int _initialSteps = -1;

        public int TotalSteps { get; private set; }
        public int Last24HoursSteps { get; private set; }

        public event EventHandler StepsUpdated;

        public async Task StartAsync()
        {
            if (!CMPedometer.IsStepCountingAvailable)
                return;

            var now = NSDate.Now;

            // Query the last 24h steps to initialize Last24HoursSteps
            var from = now.AddSeconds(-24 * 3600);
            var summary = await _pedometer.QueryPedometerDataAsync(from, now);
            Last24HoursSteps = summary?.NumberOfSteps?.Int32Value ?? 0;

            // Start live updates
            _pedometer.StartPedometerUpdates(now, (data, error) =>
            {
                if (error != null || data == null)
                    return;

                int currentSteps = data.NumberOfSteps?.Int32Value ?? 0;

                if (_initialSteps == -1)
                    _initialSteps = currentSteps;

                TotalSteps = currentSteps - _initialSteps;

                StepsUpdated?.Invoke(this, EventArgs.Empty);
            });
        }

        public Task StopAsync()
        {
            _pedometer.StopPedometerUpdates();
            return Task.CompletedTask;
        }
    }
}
