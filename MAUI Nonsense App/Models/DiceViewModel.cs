using MAUI_Nonsense_App.Services;
using System.ComponentModel;
using System.Diagnostics;

namespace MAUI_Nonsense_App.Models;

public class DiceAnimationModel
{
    public float X, Y;
    public float DX, DY;
    public float Rotation;
    public float DRotation;
    public int Value = 1;
    public bool IsRolling = true;
}

public class DiceViewModel : INotifyPropertyChanged
{
    private readonly IDiceRollService _diceRollService;
    private readonly Random _random = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public int DiceCount { get; private set; } = 1;

    public List<DiceAnimationModel> Animations { get; private set; } = new();

    public DiceViewModel(IDiceRollService diceRollService)
    {
        _diceRollService = diceRollService;
        InitAnimations();
    }

    public void IncreaseDice()
    {
        if (DiceCount < 3)
        {
            DiceCount++;
            InitAnimations();
            OnPropertyChanged(nameof(DiceCount));
        }
    }

    public void DecreaseDice()
    {
        if (DiceCount > 1)
        {
            DiceCount--;
            InitAnimations();
            OnPropertyChanged(nameof(DiceCount));
        }
    }

    private void InitAnimations()
    {
        Animations = Enumerable.Range(0, DiceCount)
            .Select(_ => new DiceAnimationModel { Value = 1 })
            .ToList();
        OnPropertyChanged(nameof(Animations));
    }

    public async Task AnimateRoll(GraphicsView canvas, Size canvasSize)
    {
        InitAnimations();

        foreach (var dice in Animations)
        {
            dice.X = _random.Next(40, (int)(canvasSize.Width - 40));
            dice.Y = _random.Next(40, (int)(canvasSize.Height - 40));
            dice.DX = (float)(_random.NextDouble() * 12 - 6); // faster!
            dice.DY = (float)(_random.NextDouble() * 12 - 6);
            dice.DRotation = (float)(_random.NextDouble() * 12 - 6);
            dice.IsRolling = true;
        }

        var watch = Stopwatch.StartNew();
        var dotUpdateInterval = TimeSpan.FromMilliseconds(250);
        var lastDotUpdate = TimeSpan.Zero;

        while (watch.Elapsed < TimeSpan.FromSeconds(2.5))
        {
            var now = watch.Elapsed;

            for (int i = 0; i < Animations.Count; i++)
            {
                var dice = Animations[i];

                dice.X += dice.DX;
                dice.Y += dice.DY;
                dice.Rotation += dice.DRotation;

                // Wall collision
                if (dice.X < 40 || dice.X > canvasSize.Width - 40)
                    dice.DX *= -1;
                if (dice.Y < 40 || dice.Y > canvasSize.Height - 40)
                    dice.DY *= -1;
            }

            // Update dice face dots every 250ms
            if (now - lastDotUpdate >= dotUpdateInterval)
            {
                foreach (var dice in Animations)
                {
                    dice.Value = _random.Next(1, 7);
                }
                lastDotUpdate = now;
            }

            ResolveDiceCollisions();
            MainThread.BeginInvokeOnMainThread(() => canvas.Invalidate());
            await Task.Delay(16);
        }

        watch.Stop();

        var values = await _diceRollService.RollAsync(DiceCount);
        for (int i = 0; i < DiceCount; i++)
        {
            Animations[i].Value = values[i];
            Animations[i].IsRolling = false;
        }

        OnPropertyChanged(nameof(Animations));
        MainThread.BeginInvokeOnMainThread(() => canvas.Invalidate());
    }

    private void ResolveDiceCollisions()
    {
        const float size = 80;

        for (int i = 0; i < Animations.Count; i++)
        {
            for (int j = i + 1; j < Animations.Count; j++)
            {
                var a = Animations[i];
                var b = Animations[j];

                float dx = a.X - b.X;
                float dy = a.Y - b.Y;
                float distSq = dx * dx + dy * dy;
                float minDist = size;

                if (distSq < minDist * minDist)
                {
                    // Swap velocity
                    (a.DX, b.DX) = (b.DX, a.DX);
                    (a.DY, b.DY) = (b.DY, a.DY);
                }
            }
        }
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
