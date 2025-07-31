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
        float sliceSize = 360f / totalSlices;
        float randomInSlice = (float)_random.NextDouble(); // fine offset within slice
        int targetSlice = _random.Next(totalSlices);

        // Make sure the angle moves forward regardless of current value
        float normalizedCurrent = CurrentAngle % 360f;
        float targetAngle = sliceSize * (totalSlices - targetSlice - randomInSlice);
        float finalAngle = 360f * fullRotations + targetAngle;

        float startAngle = normalizedCurrent;
        float endAngle = normalizedCurrent + finalAngle;
        float angleDelta = endAngle - startAngle;

        var tcs = new TaskCompletionSource();

        var animation = new Animation(
            callback: value =>
            {
                CurrentAngle = startAngle + (float)value * angleDelta;
                canvas.Invalidate();
            },
            start: 0,
            end: 1,
            easing: Easing.CubicOut
        );

        animation.Commit(canvas, "SpinAnimation", 16, 3500, finished: (v, c) => tcs.SetResult());

        await tcs.Task;

        float adjustedAngle = (360f - (CurrentAngle % 360f)) % 360f;
        int selectedIndex = (int)Math.Floor(adjustedAngle / sliceSize);

        SelectedOption = Options[selectedIndex];
        OnPropertyChanged(nameof(SelectedOption));
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
