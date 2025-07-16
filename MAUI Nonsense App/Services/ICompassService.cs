namespace MAUI_Nonsense_App.Services
{
    public interface ICompassService
    {
        event EventHandler<double> HeadingChanged;
        event EventHandler<CompassAccuracy> AccuracyChanged;

        void Start(bool highAccuracy = true);
        void Stop();
    }

    public enum CompassAccuracy
    {
        Unreliable,
        Low,
        Medium,
        High
    }
}
