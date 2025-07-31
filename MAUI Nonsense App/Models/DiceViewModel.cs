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

    private Size _canvasSize;

    public DiceViewModel(IDiceRollService diceRollService)
    {
        _diceRollService = diceRollService;
    }

    public void SetCanvasSize(Size size)
    {
        _canvasSize = size;
        InitNonOverlappingPositions();
    }

    public void IncreaseDice()
    {
        if (DiceCount < 10)
        {
            DiceCount++;
            InitNonOverlappingPositions();
            OnPropertyChanged(nameof(DiceCount));
        }
    }

    public void DecreaseDice()
    {
        if (DiceCount > 1)
        {
            DiceCount--;
            InitNonOverlappingPositions();
            OnPropertyChanged(nameof(DiceCount));
        }
    }

    private void InitNonOverlappingPositions()
    {
        const int radius = 40;
        const int maxAttempts = 100;

        Animations.Clear();

        for (int i = 0; i < DiceCount; i++)
        {
            int attempts = 0;
            bool valid = false;
            DiceAnimationModel newDie = new();

            while (!valid && attempts < maxAttempts)
            {
                float x = _random.Next(radius, (int)(_canvasSize.Width - radius));
                float y = _random.Next(radius, (int)(_canvasSize.Height - radius));

                valid = true;
                foreach (var other in Animations)
                {
                    float dx = x - other.X;
                    float dy = y - other.Y;
                    if (dx * dx + dy * dy < (radius * 2) * (radius * 2))
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    newDie.X = x;
                    newDie.Y = y;
                    newDie.Value = _random.Next(1, 7);
                    Animations.Add(newDie);
                }

                attempts++;
            }

            if (!valid)
            {
                newDie.X = 50 + i * 90;
                newDie.Y = 100;
                newDie.Value = _random.Next(1, 7);
                Animations.Add(newDie);
            }
        }

        OnPropertyChanged(nameof(Animations));
    }

    public async Task AnimateRoll(GraphicsView canvas)
    {
        const float velocityMultiplier = 4.5f;
        const float baseFriction = 0.985f;

        foreach (var dice in Animations)
        {
            dice.DX = (float)(_random.NextDouble() * 6 - 3) * velocityMultiplier;
            dice.DY = (float)(_random.NextDouble() * 6 - 3) * velocityMultiplier;
            dice.DRotation = (float)(_random.NextDouble() * 10 - 5);
            dice.IsRolling = true;
        }

        var dotUpdateInterval = TimeSpan.FromMilliseconds(250);
        var lastDotUpdate = TimeSpan.Zero;

        var stopwatch = Stopwatch.StartNew();

        // Updated phase durations
        const double fastPhase = 2.0;
        const double slowPhase = 1.0;
        const double totalDuration = fastPhase + slowPhase;

        while (stopwatch.Elapsed.TotalSeconds < totalDuration)
        {
            double t = stopwatch.Elapsed.TotalSeconds;

            float friction = t < fastPhase
                ? 1.0f
                : baseFriction - (float)((t - fastPhase) / slowPhase * 0.04f); // interpolate

            UpdateDice(canvas, friction, ref lastDotUpdate, dotUpdateInterval);
            await Task.Delay(16);
        }

        stopwatch.Stop();

        var values = await _diceRollService.RollAsync(DiceCount);
        for (int i = 0; i < DiceCount; i++)
        {
            Animations[i].Value = values[i];
            Animations[i].IsRolling = false;
        }

        OnPropertyChanged(nameof(Animations));
        MainThread.BeginInvokeOnMainThread(() => canvas.Invalidate());
    }

    private void UpdateDice(GraphicsView canvas, float friction, ref TimeSpan lastDotUpdate, TimeSpan dotUpdateInterval)
    {
        var now = DateTime.Now.TimeOfDay;

        foreach (var dice in Animations)
        {
            dice.X += dice.DX;
            dice.Y += dice.DY;
            dice.Rotation += dice.DRotation;

            if (dice.X < 40 || dice.X > _canvasSize.Width - 40)
            {
                dice.DX *= -1.1f;
                dice.DY += (float)(_random.NextDouble() - 0.5) * 2;
                dice.X = Math.Clamp(dice.X, 40, (float)_canvasSize.Width - 40);
            }

            if (dice.Y < 40 || dice.Y > _canvasSize.Height - 40)
            {
                dice.DY *= -1.1f;
                dice.DX += (float)(_random.NextDouble() - 0.5) * 2;
                dice.Y = Math.Clamp(dice.Y, 40, (float)_canvasSize.Height - 40);
            }

            dice.DX *= friction;
            dice.DY *= friction;
            dice.DRotation *= friction;
        }

        if (now - lastDotUpdate >= dotUpdateInterval)
        {
            foreach (var dice in Animations)
                dice.Value = _random.Next(1, 7);
            lastDotUpdate = now;
        }

        ResolveDiceCollisions();
        MainThread.BeginInvokeOnMainThread(() => canvas.Invalidate());
    }

    private void ResolveDiceCollisions()
    {
        const float radius = 40;

        for (int i = 0; i < Animations.Count; i++)
        {
            for (int j = i + 1; j < Animations.Count; j++)
            {
                var a = Animations[i];
                var b = Animations[j];

                float dx = b.X - a.X;
                float dy = b.Y - a.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance < radius * 2 && distance > 0)
                {
                    float overlap = radius * 2 - distance;
                    float nx = dx / distance;
                    float ny = dy / distance;

                    a.X -= nx * overlap / 2;
                    a.Y -= ny * overlap / 2;
                    b.X += nx * overlap / 2;
                    b.Y += ny * overlap / 2;

                    (a.DX, b.DX) = (b.DX, a.DX);
                    (a.DY, b.DY) = (b.DY, a.DY);
                }
            }
        }
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
