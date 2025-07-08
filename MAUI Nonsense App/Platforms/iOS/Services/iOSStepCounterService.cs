#if IOS
using CoreMotion;
using Foundation;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Services.iOS
{
    public class iOSStepCounterService : IStepCounterService
    {
        private readonly CMPedometer _pedometer = new CMPedometer();

        public int TotalSteps { get; private set; }
        public int Last24HoursSteps { get; private set; }

        public event EventHandler StepsUpdated;

        public async Task StartAsync()
        {
            if (!CMPedometer.IsStepCountingAvailable) return;

            var now = NSDate.Now;
            var yesterday = now.AddSeconds(-24 * 3600);

            var summary = await _pedometer.QueryPedometerDataAsync(yesterday, now);
            Last24HoursSteps = summary.NumberOfSteps.Int32Value;

            _pedometer.StartPedometerUpdates(NSDate.Now, (data, error) =>
            {
                if (data != null)
                {
                    TotalSteps = data.NumberOfSteps.Int32Value;
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
#endif
