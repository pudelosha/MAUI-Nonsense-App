using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.ViewModels;

public enum ArkanoidState { Ready, Running, Paused, GameOver }

public class ArkanoidViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<int>? GameOverEvent;

    // HUD
    public int Score { get; private set; }
    public int Lives { get; private set; } = 3;
    public ArkanoidState State { get; private set; } = ArkanoidState.Ready;

    // Canvas / playfield
    private Size _canvasSize;
    public RectF Playfield { get; private set; }
    private const float Margin = 8f;

    // Paddle
    private float _paddleX; // center X
    private float _paddleY;
    private float _paddleWidth;
    private float _paddleHeight;

    // Ball
    private float _ballX, _ballY;
    private float _ballDX = 180f, _ballDY = -220f; // px/sec
    private float _ballRadius = 7f;
    private float _speedMul = 1f;

    // Bricks
    private record Brick(RectF Rect, int Hits, Color Color, bool Active);
    private List<Brick> _bricks = new();

    // Loop
    private IDispatcherTimer? _timer;
    private long _lastTicks;
    private GraphicsView? _canvas;

    private readonly Random _rng = new();

    public void SetCanvasSize(Size size)
    {
        _canvasSize = size;
        Playfield = new RectF(
            (float)Margin,
            (float)Margin,
            (float)size.Width - 2 * Margin,
            (float)size.Height - 2 * Margin);

        if (State == ArkanoidState.Ready || State == ArkanoidState.GameOver)
            InitRound(false);
    }

    public void Start(GraphicsView canvas)
    {
        _canvas = canvas;

        if (State == ArkanoidState.Ready || State == ArkanoidState.GameOver)
        {
            Lives = 3;
            Score = 0;
            InitRound(true);
        }

        State = ArkanoidState.Running;
        EnsureTimer();
        _lastTicks = Stopwatch.GetTimestamp();
        _timer!.Start();
        NotifyHud();
    }

    public void Resume()
    {
        if (State != ArkanoidState.Paused) return;
        State = ArkanoidState.Running;
        EnsureTimer();
        _lastTicks = Stopwatch.GetTimestamp();
        _timer!.Start();
        NotifyHud();
    }

    public void Pause()
    {
        _timer?.Stop();
        if (State == ArkanoidState.Running)
            State = ArkanoidState.Paused;
        NotifyHud();
    }

    public void Reset()
    {
        _timer?.Stop();
        State = ArkanoidState.Ready;
        Score = 0;
        Lives = 3;
        InitRound(false);
        NotifyHud();
        Invalidate();
    }

    public void NudgePaddle(int dir)
    {
        MovePaddleBy(dir * Math.Max(20f, _paddleWidth * 0.25f));
    }

    public void MovePaddleBy(float dx)
    {
        if (Playfield.Width <= 0) return;
        _paddleX = Math.Clamp(_paddleX + dx, Playfield.Left + _paddleWidth / 2f, Playfield.Right - _paddleWidth / 2f);
        Invalidate();
    }

    private void InitRound(bool withBricks)
    {
        _paddleWidth = Math.Max(50f, Playfield.Width * 0.22f);
        _paddleHeight = 10f;
        _paddleX = Playfield.Center.X;
        _paddleY = Playfield.Bottom - 24f;

        _ballRadius = Math.Clamp(Playfield.Width * 0.018f, 6f, 10f);
        _ballX = _paddleX;
        _ballY = _paddleY - _ballRadius - 2f;
        _ballDX = 160f * (_rng.Next(2) == 0 ? -1 : 1);
        _ballDY = -220f;
        _speedMul = 1f;

        if (withBricks || _bricks.Count == 0)
            BuildBricks();

        Invalidate();
    }

    private void BuildBricks()
    {
        _bricks.Clear();

        int rows = 6;
        int cols = 10;
        float gap = 4f;

        float top = Playfield.Top + 30f;
        float usableW = Playfield.Width - (cols + 1) * gap;
        float bw = usableW / cols;
        float bh = Math.Max(14f, Playfield.Height * 0.035f);

        var palette = new[] { Colors.Orange, Colors.Goldenrod, Colors.CornflowerBlue, Colors.MediumSeaGreen, Colors.IndianRed, Colors.MediumPurple };

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float x = Playfield.Left + gap + c * (bw + gap);
                float y = top + r * (bh + gap);
                var rect = new RectF(x, y, bw, bh);
                var color = palette[r % palette.Length];
                _bricks.Add(new Brick(rect, 1, color, true));
            }
        }
    }

    private void EnsureTimer()
    {
        _timer ??= Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = Stopwatch.GetTimestamp();
        double dt = (now - _lastTicks) / (double)Stopwatch.Frequency;
        _lastTicks = now;

        if (State != ArkanoidState.Running) return;

        UpdateBall((float)dt);
        Invalidate();
    }

    private void UpdateBall(float dt)
    {
        float vx = _ballDX * _speedMul;
        float vy = _ballDY * _speedMul;

        float nextX = _ballX + vx * dt;
        float nextY = _ballY + vy * dt;

        // Walls
        if (nextX - _ballRadius < Playfield.Left)
        {
            nextX = Playfield.Left + _ballRadius;
            _ballDX = Math.Abs(_ballDX);
        }
        else if (nextX + _ballRadius > Playfield.Right)
        {
            nextX = Playfield.Right - _ballRadius;
            _ballDX = -Math.Abs(_ballDX);
        }

        if (nextY - _ballRadius < Playfield.Top)
        {
            nextY = Playfield.Top + _ballRadius;
            _ballDY = Math.Abs(_ballDY);
        }

        // Paddle
        var paddleRect = new RectF(_paddleX - _paddleWidth / 2f, _paddleY - _paddleHeight / 2f, _paddleWidth, _paddleHeight);
        if (CircleIntersectsRect(nextX, nextY, _ballRadius, paddleRect) && _ballDY > 0)
        {
            nextY = paddleRect.Top - _ballRadius;

            float rel = (nextX - paddleRect.Center.X) / (_paddleWidth / 2f);
            rel = Math.Clamp(rel, -1f, 1f);
            float angle = MathF.PI * (0.25f + 0.5f * (rel + 1f) / 2f); // 45°..135°
            float speed = MathF.Sqrt(_ballDX * _ballDX + _ballDY * _ballDY);
            _ballDX = speed * MathF.Cos(angle);
            _ballDY = -Math.Abs(speed * MathF.Sin(angle));
        }

        // Bricks
        for (int i = 0; i < _bricks.Count; i++)
        {
            if (!_bricks[i].Active) continue;
            var b = _bricks[i];

            if (CircleIntersectsRect(nextX, nextY, _ballRadius, b.Rect))
            {
                float left = Math.Abs((b.Rect.Left - (nextX + _ballRadius)));
                float right = Math.Abs(((nextX - _ballRadius) - b.Rect.Right));
                float top = Math.Abs((b.Rect.Top - (nextY + _ballRadius)));
                float bottom = Math.Abs(((nextY - _ballRadius) - b.Rect.Bottom));

                float min = Math.Min(Math.Min(left, right), Math.Min(top, bottom));
                if (min == left || min == right) _ballDX = -_ballDX;
                else _ballDY = -_ballDY;

                _bricks[i] = b with { Hits = b.Hits - 1, Active = (b.Hits - 1) > 0 };
                Score += 10;
                _speedMul = Math.Min(1.8f, _speedMul + 0.02f);
                NotifyHud();
                break;
            }
        }

        // Lose life: pause and wait for input (no auto-restart)
        if (nextY - _ballRadius > Playfield.Bottom)
        {
            Lives--;
            NotifyHud();

            if (Lives <= 0)
            {
                State = ArkanoidState.GameOver;
                _timer?.Stop();
                GameOverEvent?.Invoke(Score);
                return;
            }

            // Place a new ball above the paddle and PAUSE
            _ballX = _paddleX;
            _ballY = _paddleY - _ballRadius - 2f;
            _ballDX = 160f * (_rng.Next(2) == 0 ? -1 : 1);
            _ballDY = -220f;
            _speedMul = Math.Max(1f, _speedMul * 0.95f);

            State = ArkanoidState.Paused;
            _timer?.Stop();
            return;
        }

        // Win level
        if (_bricks.All(br => !br.Active))
        {
            BuildBricks();
            _speedMul = 1f;
        }

        _ballX = nextX;
        _ballY = nextY;
    }

    private static bool CircleIntersectsRect(float cx, float cy, float r, RectF rect)
    {
        float clampedX = Math.Clamp(cx, rect.Left, rect.Right);
        float clampedY = Math.Clamp(cy, rect.Top, rect.Bottom);
        float dx = cx - clampedX;
        float dy = cy - clampedY;
        return dx * dx + dy * dy <= r * r;
    }

    private void Invalidate() =>
        MainThread.BeginInvokeOnMainThread(() => _canvas?.Invalidate());

    private void NotifyHud()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Score)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Lives)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
    }

    // Exposed to the drawable
    public RectF PaddleRect => new(_paddleX - _paddleWidth / 2f, _paddleY - _paddleHeight / 2f, _paddleWidth, _paddleHeight);
    public (float x, float y) Ball => (_ballX, _ballY);
    public float BallRadius => _ballRadius;
    public IReadOnlyList<(RectF rect, Color color, bool active)> Bricks =>
        _bricks.Select(b => (b.Rect, b.Color, b.Active)).ToList();
}
