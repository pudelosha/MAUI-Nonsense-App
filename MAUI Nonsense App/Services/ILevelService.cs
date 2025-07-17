namespace MAUI_Nonsense_App.Services
{
    public interface ILevelService
    {
        event EventHandler<LevelReading> ReadingChanged;

        Task StartAsync();
        Task StopAsync();
    }

    public record LevelReading(double Pitch, double Roll);
}
