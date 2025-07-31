using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.Models;

public class RouletteViewModel : INotifyPropertyChanged
{
    private readonly Random _random = new();
    private float _ballAngle;
    private bool _isAmerican;

    public event PropertyChangedEventHandler? PropertyChanged;

    public List<RouletteSlot> Slots { get; private set; } = new();
    public string SelectedSlot { get; private set; } = "";

    public float BallAngle
    {
        get => _ballAngle;
        private set
        {
            _ballAngle = value;
            OnPropertyChanged(nameof(BallAngle));
        }
    }

    public bool IsAmerican
    {
        get => _isAmerican;
        set
        {
            if (_isAmerican != value)
            {
                _isAmerican = value;
                ResetWheel();
                OnPropertyChanged(nameof(IsAmerican));
            }
        }
    }

    public RouletteViewModel()
    {
        ResetWheel();
    }

    public void ResetWheel()
    {
        Slots = _isAmerican ? RouletteData.American : RouletteData.European;
        OnPropertyChanged(nameof(Slots));
    }

    public async Task SpinBall(GraphicsView canvas)
    {
        int count = Slots.Count;
        float slice = 360f / count;
        int resultIndex = _random.Next(count);
        float finalAngle = slice * resultIndex + (float)(_random.NextDouble() * slice);

        const int duration = 3000;
        float totalRotation = 5 * 360f + finalAngle;
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < duration)
        {
            float t = (float)stopwatch.ElapsedMilliseconds / duration;
            float eased = 1f - (float)Math.Pow(1 - t, 3);
            BallAngle = totalRotation * eased;

            MainThread.BeginInvokeOnMainThread(() => canvas.Invalidate());
            await Task.Delay(16);
        }

        BallAngle = totalRotation;
        SelectedSlot = Slots[resultIndex].Label;
        OnPropertyChanged(nameof(SelectedSlot));
        MainThread.BeginInvokeOnMainThread(() => canvas.Invalidate());
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public record RouletteSlot(string Label, Color Color);

public static class RouletteData
{
    public static readonly List<RouletteSlot> European = new()
    {
        new("0", Colors.Green), new("32", Colors.Red), new("15", Colors.Black), new("19", Colors.Red),
        new("4", Colors.Black), new("21", Colors.Red), new("2", Colors.Black), new("25", Colors.Red),
        new("17", Colors.Black), new("34", Colors.Red), new("6", Colors.Black), new("27", Colors.Red),
        new("13", Colors.Black), new("36", Colors.Red), new("11", Colors.Black), new("30", Colors.Red),
        new("8", Colors.Black), new("23", Colors.Red), new("10", Colors.Black), new("5", Colors.Red),
        new("24", Colors.Black), new("16", Colors.Red), new("33", Colors.Black), new("1", Colors.Red),
        new("20", Colors.Black), new("14", Colors.Red), new("31", Colors.Black), new("9", Colors.Red),
        new("22", Colors.Black), new("18", Colors.Red), new("29", Colors.Black), new("7", Colors.Red),
        new("28", Colors.Black), new("12", Colors.Red), new("35", Colors.Black), new("3", Colors.Red),
        new("26", Colors.Black)
    };

    public static readonly List<RouletteSlot> American = new()
    {
        new("0", Colors.Green), new("00", Colors.Green), new("1", Colors.Red), new("2", Colors.Black),
        new("3", Colors.Red), new("4", Colors.Black), new("5", Colors.Red), new("6", Colors.Black),
        new("7", Colors.Red), new("8", Colors.Black), new("9", Colors.Red), new("10", Colors.Black),
        new("11", Colors.Black), new("12", Colors.Red), new("13", Colors.Black), new("14", Colors.Red),
        new("15", Colors.Black), new("16", Colors.Red), new("17", Colors.Black), new("18", Colors.Red),
        new("19", Colors.Red), new("20", Colors.Black), new("21", Colors.Red), new("22", Colors.Black),
        new("23", Colors.Red), new("24", Colors.Black), new("25", Colors.Red), new("26", Colors.Black),
        new("27", Colors.Red), new("28", Colors.Black), new("29", Colors.Black), new("30", Colors.Red),
        new("31", Colors.Black), new("32", Colors.Red), new("33", Colors.Black), new("34", Colors.Red),
        new("35", Colors.Black), new("36", Colors.Red)
    };
}
