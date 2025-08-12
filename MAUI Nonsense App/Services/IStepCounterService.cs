namespace MAUI_Nonsense_App.Services
{
    public interface IStepCounterService
    {
        int TotalSteps { get; }
        int Last24HoursSteps { get; }
        long ActiveSecondsToday { get; }
        Dictionary<string, int> StepHistory { get; }
        event EventHandler? StepsUpdated;
        Task StartAsync();
        Task StopAsync();
        void ResetAll();
        void RaiseStepsUpdated();
    }
}