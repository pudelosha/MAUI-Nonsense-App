namespace MAUI_Nonsense_App.Services
{
    public interface IStepCounterService
    {
        Task StartAsync();
        Task StopAsync();

        /// <summary> Total steps counted since app started </summary>
        int TotalSteps { get; }

        /// <summary> Steps counted in the past 24 hours </summary>
        int Last24HoursSteps { get; }

        /// <summary> Event to notify when step count changes </summary>
        event EventHandler StepsUpdated;
    }
}
