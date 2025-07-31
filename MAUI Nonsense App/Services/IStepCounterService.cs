public interface IStepCounterService
{
    int TotalSteps { get; }
    int Last24HoursSteps { get; }
    Dictionary<string, int> StepHistory { get; }

    int RawSensorValue { get; } // NEW (optional for debugging)

    void SaveDailySnapshotIfNeeded(int currentSensorValue);
    Task StartAsync();
    Task StopAsync();
    void ResetAll();
}
