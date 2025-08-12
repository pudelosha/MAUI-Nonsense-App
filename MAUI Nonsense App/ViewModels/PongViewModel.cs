using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.ViewModels;

public enum PongState { Ready, Running, RoundPaused, GameOver }

public class PongViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<string>? GameOverEvent;

    // HUD
    public int PlayerScore { get; private set; }
    public int CpuScore { get; private set; }
    public PongState State { get; private set; } = PongState.Ready;

    // Playfield
    public RectF Playfield { get; private set; }
    private const float Margin = 8f;

    // Paddles
    private float _paddleW, _paddleH;
    private float _playerX, _playerY; // centers
    private float _cpuX, _cpuY;

    // Ball
    private float _ballX, _ballY;
    private float _ballDX, _ballDY;
    private float _ballR;

    // Speeds / rules
    private float _ballSpeed = 260f;
    private float _cpuMaxSpeed = 260f;
    private const int WinScore = 7;

    // Bounce shaping
    private const float MinHorizFrac = 0.7f;
    private const float PaddleMinDeg = 4f;
    private const float PaddleMaxDeg = 16f;

    // Loop
    private IDispatcherTimer? _timer;
    private long _lastTicks;
    private GraphicsView? _canvas;
    private readonly Random _rng = new();

    public void SetCanvasSize(Size size)
    {
        Playfield = new RectF(
            (float)Margin, (float)Margin,
            (float)size.Width - 2f * Margin,
            (float)size.Height - 2f * Margin);

        if (State == PongState.Ready || State == PongState.GameOver)
            LayoutObjects();
    }

    public void Ready()
    {
        PlayerScore = 0; CpuScore = 0;
        State = PongState.Ready;
        LayoutObjects();
        NotifyHud(); Invalidate();
    }

    public void Start(GraphicsView canvas)
    {
        _canvas = canvas;
        if (State == PongState.Ready || State == PongState.GameOver)
        {
            PlayerScore = 0; CpuScore = 0;
            LayoutObjects();
        }
        LaunchBall();
        State = PongState.Running;
        EnsureTimer();
        _lastTicks = Stopwatch.GetTimestamp();
        _timer!.Start();
        NotifyHud();
    }

    public void StartOrResume(GraphicsView canvas)
    {
        _canvas = canvas;
        if (State == PongState.Ready || State == PongState.GameOver)
        {
            PlayerScore = 0; CpuScore = 0;
            LayoutObjects();
            LaunchBall();
        }
        else if (State == PongState.RoundPaused)
        {
            LaunchBall();
        }
        State = PongState.Running;
        EnsureTimer();
        _lastTicks = Stopwatch.GetTimestamp();
        _timer!.Start();
        NotifyHud();
    }

    public void Pause()
    {
        _timer?.Stop();
        if (State == PongState.Running) State = PongState.RoundPaused;
        NotifyHud();
    }

    public void NudgePlayer(int dir) => MovePlayerBy(dir * Math.Max(18f, _paddleH * 0.25f));

    public void MovePlayerBy(float dy)
    {
        _playerY = Math.Clamp(_playerY + dy,
            Playfield.Top + _paddleH / 2f,
            Playfield.Bottom - _paddleH / 2f);
        Invalidate();
    }

    private void LayoutObjects()
    {
        _paddleW = Math.Max(8f, Playfield.Width * 0.018f);
        _paddleH = Math.Max(60f, Playfield.Height * 0.18f);
        _ballR = Math.Clamp(Playfield.Width * 0.012f, 6f, 10f);

        _playerX = Playfield.Left + 18f;
        _cpuX = Playfield.Right - 18f;
        _playerY = _cpuY = Playfield.Center.Y;

        _ballX = Playfield.Center.X;
        _ballY = Playfield.Center.Y;
        _ballDX = _ballDY = 0;

        _ballSpeed = Math.Max(220f, Playfield.Height * 0.45f);
        _cpuMaxSpeed = _ballSpeed * 0.9f;
    }

    private void LaunchBall()
    {
        _ballX = Playfield.Center.X;
        _ballY = Playfield.Center.Y;

        // 30..150° from +X axis (roughly), then enforce horizontal dominance
        float angle = (float)(Math.PI / 6 + _rng.NextDouble() * (2.5 * Math.PI / 3)); // 30°..150°
        float dir = _rng.Next(2) == 0 ? -1f : 1f;

        _ballDX = dir * _ballSpeed * MathF.Cos(angle);
        _ballDY = (_rng.Next(2) == 0 ? -1f : 1f) * _ballSpeed * MathF.Sin(angle);

        EnsureMinHorizontal(ref _ballDX, ref _ballDY, MinHorizFrac);
    }

    private void EnsureTimer()
    {
        _timer ??= Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16); // ~60fps
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = Stopwatch.GetTimestamp();
        float dt = (float)((now - _lastTicks) / (double)Stopwatch.Frequency);
        _lastTicks = now;

        if (State != PongState.Running) return;

        UpdateCpu(dt);
        UpdateBall(dt);
        Invalidate();
    }

    private void UpdateCpu(float dt)
    {
        float targetY = _ballY;
        float dy = targetY - _cpuY;
        float max = _cpuMaxSpeed * dt;
        dy = Math.Clamp(dy, -max, max);
        _cpuY = Math.Clamp(_cpuY + dy,
            Playfield.Top + _paddleH / 2f,
            Playfield.Bottom - _paddleH / 2f);
    }

    private void UpdateBall(float dt)
    {
        float nx = _ballX + _ballDX * dt;
        float ny = _ballY + _ballDY * dt;

        // Top/bottom walls — reflect and enforce horizontal floor
        if (ny - _ballR < Playfield.Top)
        {
            ny = Playfield.Top + _ballR;
            _ballDY = Math.Abs(_ballDY);
            EnsureMinHorizontal(ref _ballDX, ref _ballDY, MinHorizFrac);
        }
        else if (ny + _ballR > Playfield.Bottom)
        {
            ny = Playfield.Bottom - _ballR;
            _ballDY = -Math.Abs(_ballDY);
            EnsureMinHorizontal(ref _ballDX, ref _ballDY, MinHorizFrac);
        }

        var pRect = PlayerRect;
        var cRect = CpuRect;

        // Left paddle
        if (_ballDX < 0 && CircleIntersectsRect(nx, ny, _ballR, pRect))
        {
            nx = pRect.Right + _ballR;
            BounceFromPaddle(ref _ballDX, ref _ballDY, nx, ny, pRect, leftPaddle: true);
            SpeedUp();
        }
        // Right paddle
        else if (_ballDX > 0 && CircleIntersectsRect(nx, ny, _ballR, cRect))
        {
            nx = cRect.Left - _ballR;
            BounceFromPaddle(ref _ballDX, ref _ballDY, nx, ny, cRect, leftPaddle: false);
            SpeedUp();
        }

        // Goals
        if (nx + _ballR < Playfield.Left)
        {
            CpuScore++; RoundPauseOrGameOver(); return;
        }
        if (nx - _ballR > Playfield.Right)
        {
            PlayerScore++; RoundPauseOrGameOver(); return;
        }

        // Final safety each frame (prevents drifting vertical)
        EnsureMinHorizontal(ref _ballDX, ref _ballDY, MinHorizFrac);

        _ballX = nx; _ballY = ny;
    }

    private void RoundPauseOrGameOver()
    {
        NotifyHud();
        if (PlayerScore >= WinScore || CpuScore >= WinScore)
        {
            State = PongState.GameOver;
            _timer?.Stop();
            string result = PlayerScore > CpuScore ? "You win!" : "CPU wins!";
            GameOverEvent?.Invoke($"{result}\nFinal score {PlayerScore}:{CpuScore}");
        }
        else
        {
            State = PongState.RoundPaused;
            _timer?.Stop();
            LayoutObjects(); // center paddles & ball, stop ball
        }
    }

    private static bool CircleIntersectsRect(float cx, float cy, float r, RectF rect)
    {
        float clampedX = Math.Clamp(cx, rect.Left, rect.Right);
        float clampedY = Math.Clamp(cy, rect.Top, rect.Bottom);
        float dx = cx - clampedX;
        float dy = cy - clampedY;
        return dx * dx + dy * dy <= r * r;
    }

    // Paddle bounce with shallow angles from the horizontal (5°..20°)
    private void BounceFromPaddle(ref float vx, ref float vy, float bx, float by, RectF paddle, bool leftPaddle)
    {
        float rel = (by - paddle.Center.Y) / (paddle.Height / 2f); // -1..1 (top..bottom)
        rel = Math.Clamp(rel, -1f, 1f);

        float alphaDeg = PaddleMinDeg + (PaddleMaxDeg - PaddleMinDeg) * MathF.Abs(rel);
        float alpha = alphaDeg * (MathF.PI / 180f);

        float speed = MathF.Sqrt(vx * vx + vy * vy);
        float dir = leftPaddle ? 1f : -1f;                   // send ball toward opposite side
        float vySign = rel == 0 ? (vy >= 0 ? 1f : -1f) : MathF.Sign(rel);

        vx = dir * speed * MathF.Cos(alpha);
        vy = vySign * speed * MathF.Sin(alpha);

        EnsureMinHorizontal(ref vx, ref vy, MinHorizFrac);
    }

    private void SpeedUp()
    {
        float speed = MathF.Sqrt(_ballDX * _ballDX + _ballDY * _ballDY);
        speed = Math.Min(speed * 1.04f, _ballSpeed * 1.7f);
        float ang = MathF.Atan2(_ballDY, _ballDX);
        _ballDX = speed * MathF.Cos(ang);
        _ballDY = speed * MathF.Sin(ang);
    }

    // Keep a minimum horizontal component so the ball never goes near-vertical
    private void EnsureMinHorizontal(ref float vx, ref float vy, float minFrac)
    {
        float speed = MathF.Sqrt(vx * vx + vy * vy);
        if (speed <= 0) return;

        float min = speed * minFrac;              // e.g., 85% of total speed
        if (MathF.Abs(vx) < min)
        {
            float sign = MathF.Sign(vx);
            if (sign == 0) sign = 1f;
            vx = min * sign;

            // conserve total speed
            float vyMag = MathF.Sqrt(MathF.Max(1f, speed * speed - vx * vx));
            vy = MathF.Sign(vy) * vyMag;
        }
    }

    private void Invalidate() =>
        MainThread.BeginInvokeOnMainThread(() => _canvas?.Invalidate());

    private void NotifyHud()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlayerScore)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CpuScore)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
    }

    // Exposed to the drawable
    public RectF PlayerRect => new(_playerX - _paddleW / 2f, _playerY - _paddleH / 2f, _paddleW, _paddleH);
    public RectF CpuRect => new(_cpuX - _paddleW / 2f, _cpuY - _paddleH / 2f, _paddleW, _paddleH);
    public (float x, float y) Ball => (_ballX, _ballY);
    public float BallRadius => _ballR;
}
