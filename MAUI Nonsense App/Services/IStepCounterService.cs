namespace MAUI_Nonsense_App.Services
{
    public interface IStepCounterService
    {
        Task StartAsync();
        Task StopAsync();

        int TotalSteps { get; }

        int Last24HoursSteps { get; }

        event EventHandler StepsUpdated;
    }
}
