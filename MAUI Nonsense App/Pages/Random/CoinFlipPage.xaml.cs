using MAUI_Nonsense_App.Pages._Drawable;
using Microsoft.Maui.Dispatching;

namespace MAUI_Nonsense_App.Pages.Random;

public partial class CoinFlipPage : ContentPage
{
    private readonly CoinFlipDrawable _drawable;
    private readonly IDispatcherTimer _timer;

    private double _angle;
    private int _steps;
    private bool _resultIsHeads;

    private int _headsCount = 0;
    private int _tailsCount = 0;
    private readonly Queue<string> _lastResults = new();
    private readonly System.Random _rng = new();

    public CoinFlipPage()
    {
        InitializeComponent();

        _drawable = new CoinFlipDrawable();
        CoinCanvas.Drawable = _drawable;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16); // ~60 fps
        _timer.Tick += OnAnimationStep;

        UpdateStats();
    }

    private void OnTossClicked(object sender, EventArgs e)
    {
        _angle = 0;
        _steps = 60;
        _resultIsHeads = _rng.Next(2) == 0;

        ResultLabel.Text = "Tossing…";
        _timer.Start();
    }

    private void OnAnimationStep(object? sender, EventArgs e)
    {
        if (_steps-- > 0)
        {
            _angle = (_angle + 30) % 360;
            bool showingFront = Math.Cos(_angle * Math.PI / 180) > 0;
            _drawable.Face = showingFront ? "Heads" : "Tails";
            _drawable.CurrentAngle = _angle;
            CoinCanvas.Invalidate();
            return;
        }

        _timer.Stop();

        string finalResult = _resultIsHeads ? "Heads" : "Tails";
        _drawable.Face = finalResult;
        _drawable.CurrentAngle = 0;
        CoinCanvas.Invalidate();

        ResultLabel.Text = finalResult;

        if (finalResult == "Heads") _headsCount++; else _tailsCount++;
        _lastResults.Enqueue(finalResult);
        if (_lastResults.Count > 5) _lastResults.Dequeue();

        UpdateStats();
    }

    private void UpdateStats()
    {
        int total = _headsCount + _tailsCount;
        string split = total > 0
            ? $"{_headsCount * 100 / total}% Heads, {_tailsCount * 100 / total}% Tails"
            : "0% Heads, 0% Tails";

        SplitLabel.Text = split;
        LastResultsLabel.Text = _lastResults.Count > 0
            ? string.Join(", ", _lastResults.Reverse())
            : "-";
    }
}
