using System.ComponentModel;
using System.Diagnostics;

namespace MAUI_Nonsense_App.Models;

public class SpinnerViewModel : INotifyPropertyChanged
{
    private readonly Random _random = new();
    private float _currentAngle = 0f;

    public event PropertyChangedEventHandler? PropertyChanged;

    public List<string> Options { get; private set; } = Enumerable.Range(1, 8).Select(n => n.ToString()).ToList();
    public string SelectedOption { get; private set; } = "";

    public float CurrentAngle
    {
        get => _currentAngle;
        private set
        {
            _currentAngle = value;
            OnPropertyChanged(nameof(CurrentAngle));
        }
    }

    public void SetOptions(List<string> options)
    {
        Options = options;
        OnPropertyChanged(nameof(Options));
    }

    public async Task Spin(GraphicsView canvas)
    {
        int totalSlices = Options.Count;
        float fullRotations = 5;
        float targetSlice = _random.Next(totalSlices);
        float finalAngle = 360f * fullRotations + (360f / totalSlices) * (totalSlices - targetSlice - 0.5f);

        const int duration = 3500;
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < duration)
        {
            float progress = (float)stopwatch.ElapsedMilliseconds / duration;
            float easedProgress = 1f - (float)Math.Pow(1 - progress, 3); // Ease out
            CurrentAngle = finalAngle * easedProgress;

            MainThread.BeginInvokeOnMainThread(() => canvas.Invalidate());
            await Task.Delay(16);
        }

        CurrentAngle = finalAngle;
        MainThread.BeginInvokeOnMainThread(() => canvas.Invalidate());

        SelectedOption = Options[(int)Math.Floor(((360f - (CurrentAngle % 360f)) % 360f) / (360f / totalSlices))];
        OnPropertyChanged(nameof(SelectedOption));
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
