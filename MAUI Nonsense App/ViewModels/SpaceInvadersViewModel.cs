using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.ViewModels;

public enum SIState { Ready, Running, Paused, GameOver }

public class SpaceInvadersViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<int, int>? GameOverEvent; // score, wave

    // HUD
    public int Score { get; private set; }
    public int Lives { get; private set; } = 3;
    public int Wave { get; private set; } = 1;
    public SIState State { get; private set; } = SIState.Ready;

    // Playfield
    public RectF Playfield { get; private set; }
    private const float Margin = 8f;

    // Player
    private float _shipX, _shipY, _shipW, _shipH;

    // Aliens (grid, but we render individually)
    private record Alien(RectF Rect, bool Alive, int Row);
    private readonly System.Collections.Generic.List<Alien> _aliens = new();
    private int _rows = 5, _cols = 10;
    private int _initialAliens;

    // Classic marching (SIDE-TO-SIDE ONLY)
    private float _hStep;           // horizontal pixels per step
    private int _direction = 1;   // +1 = right, -1 = left
    private float _stepInterval;    // seconds between steps
    private float _stepTimer;

    // Projectiles
    private record Shot(float X, float Y, float W, float H, float Vy, bool FromPlayer);
    private readonly System.Collections.Generic.List<Shot> _shots = new();

    private float _playerFireCooldown = 0f;

    // Enemy fire system (more shots)
    private float _enemyTimer = 0f;
    private const int MaxEnemyShotsOnScreen = 3; // allow more bullets on screen

    // Loop
    private IDispatcherTimer? _timer;
    private long _lastTicks;
    private GraphicsView? _canvas;
    private readonly Random _rng = new();

    public void SetCanvasSize(Size size)
    {
        Playfield = new RectF(
            (float)Margin,
            (float)Margin,
            (float)size.Width - 2f * Margin,
            (float)size.Height - 2f * Margin);

        if (State == SIState.Ready || State == SIState.GameOver)
            LayoutEntitiesForWave();
    }

    public void Ready()
    {
        State = SIState.Ready;
        Score = 0;
        Lives = 3;
        Wave = 1;
        _shots.Clear();
        LayoutEntitiesForWave();
        NotifyHud();
        Invalidate();
    }

    public void Start(GraphicsView canvas)
    {
        _canvas = canvas;
        if (State == SIState.Ready || State == SIState.GameOver)
        {
            Score = 0; Lives = 3; Wave = 1;
            _shots.Clear();
            LayoutEntitiesForWave();
        }
        State = SIState.Running;
        EnsureTimer();
        _lastTicks = Stopwatch.GetTimestamp();
        _timer!.Start();
        NotifyHud();
    }

    public void Resume()
    {
        if (State != SIState.Paused) return;
        State = SIState.Running;
        EnsureTimer();
        _lastTicks = Stopwatch.GetTimestamp();
        _timer!.Start();
        NotifyHud();
    }

    public void Pause()
    {
        _timer?.Stop();
        if (State == SIState.Running) State = SIState.Paused;
        NotifyHud();
    }

    // Movement controls
    public void Nudge(int dir) => MoveBy(dir * Math.Max(18f, _shipW * 0.35f));
    public void MoveBy(float dx)
    {
        _shipX = Math.Clamp(_shipX + dx, Playfield.Left + _shipW / 2f, Playfield.Right - _shipW / 2f);
        Invalidate();
    }

    public void Fire()
    {
        if (State != SIState.Running) return;
        if (_playerFireCooldown > 0f) return;

        float w = Math.Max(3f, _shipW * 0.14f);
        float h = Math.Max(10f, _shipH * 0.9f);
        _shots.Add(new Shot(_shipX - w / 2f, _shipY - _shipH / 2f - h, w, h, -560f, true));

        _playerFireCooldown = 0.18f; // seconds between shots
        Invalidate();
    }

    private void LayoutEntitiesForWave()
    {
        // Player ship size/pos
        _shipW = Math.Max(36f, Playfield.Width * 0.09f);
        _shipH = Math.Max(14f, Playfield.Height * 0.04f);
        _shipX = Playfield.Center.X;
        _shipY = Playfield.Bottom - _shipH * 1.5f;

        // Aliens grid
        _aliens.Clear();

        int rows = _rows;
        int cols = _cols;

        float gapX = Math.Max(6f, Playfield.Width * 0.010f);
        float gapY = Math.Max(6f, Playfield.Height * 0.012f);

        float totalGapX = (cols + 1) * gapX;
        float usableW = Playfield.Width - totalGapX;
        float alienW = usableW / cols;
        float alienH = Math.Max(16f, Playfield.Height * 0.035f);

        // Start HIGH on the board
        float top = Playfield.Top + Playfield.Height * 0.22f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float x = Playfield.Left + gapX + c * (alienW + gapX);
                float y = top + r * (alienH + gapY);
                _aliens.Add(new Alien(new RectF(x, y, alienW, alienH), true, r));
            }
        }

        _initialAliens = rows * cols;

        // Side-to-side marching
        _hStep = Math.Max(6f, alienW * 0.30f);              // ~⅓ alien width per step
        _direction = 1;                                         // start right
        _stepInterval = 0.55f / Math.Max(1f, (1f + 0.05f * (Wave - 1)));
        _stepTimer = 0f;

        // Enemy fire starts fast-ish; ramps slightly with wave
        _enemyTimer = 0.5f;
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

        if (State != SIState.Running) return;
        dt = MathF.Min(dt, 0.033f); // clamp big jumps

        UpdateCooldowns(dt);
        UpdateAliens(dt);
        UpdateShots(dt);
        CheckWaveOrGameOver();

        Invalidate();
    }

    private void UpdateCooldowns(float dt)
    {
        if (_playerFireCooldown > 0f) _playerFireCooldown -= dt;

        // ENEMY FIRE: scale by remaining aliens (density)
        _enemyTimer -= dt;
        if (_enemyTimer <= 0f && _aliens.Any(a => a.Alive))
        {
            int alive = _aliens.Count(a => a.Alive);
            float density = Math.Clamp(alive / (float)_initialAliens, 0f, 1f); // 0..1
            int onScreen = _shots.Count(s => !s.FromPlayer);

            // How many enemy bullets allowed concurrently? More when many aliens, fewer when few.
            // Range: ~25%..100% of the base cap, at least 1.
            int allowed = Math.Max(1, (int)MathF.Ceiling(MaxEnemyShotsOnScreen * (0.25f + 0.75f * density)));

            // Spawn up to the gap to allowed, but avoid sudden bursts: cap per volley to 2.
            int gap = Math.Max(0, allowed - onScreen);
            int spawn = Math.Min(gap, 2);
            for (int i = 0; i < spawn; i++)
                FireEnemyShot();

            // Next volley interval: fast when many aliens, slower when few.
            // Lerp(min, max, 1 - density): density=1 -> min (fast), density=0 -> max (slow)
            float minInt = 0.20f, maxInt = 0.60f;
            float baseInterval = Lerp(minInt, maxInt, 1f - density);

            // Mild wave speed-up, but never crazy
            float waveFactor = 1f / Math.Max(1f, (1f + 0.10f * (Wave - 1))); // higher wave -> a bit faster
            _enemyTimer = MathF.Max(0.20f, baseInterval * waveFactor);
        }
    }

    private void FireEnemyShot()
    {
        var alive = _aliens.Where(a => a.Alive).ToList();
        if (!alive.Any()) return;

        // choose a random alive alien (weighted to those nearer the bottom)
        var shooter = alive
            .OrderByDescending(a => a.Rect.Bottom + _rng.NextSingle() * 10f)
            .Skip(_rng.Next(Math.Max(1, alive.Count / 4))) // randomness
            .First();

        float w = Math.Max(3f, shooter.Rect.Width * 0.18f);
        float h = Math.Max(10f, shooter.Rect.Height * 0.9f);
        float speed = 260f + Wave * 30f; // faster on higher waves
        _shots.Add(new Shot(shooter.Rect.Center.X - w / 2f, shooter.Rect.Bottom, w, h, +speed, false));
    }

    // SIDE-TO-SIDE marching only; reverse at walls, no vertical drop
    private void UpdateAliens(float dt)
    {
        if (_aliens.Count == 0) return;

        int alive = _aliens.Count(a => a.Alive);
        if (alive == 0) return;

        // Speed up steps as aliens die (classic feel)
        float aliveFrac = alive / (float)_initialAliens;
        float t = 1f - aliveFrac;
        _stepInterval = Lerp(0.55f, 0.18f, t) / Math.Max(1f, (1f + 0.05f * (Wave - 1)));

        _stepTimer += dt;
        while (_stepTimer >= _stepInterval)
        {
            _stepTimer -= _stepInterval;
            MarchStepHorizontal();
        }
    }

    private void MarchStepHorizontal()
    {
        float leftMost = _aliens.Where(a => a.Alive).Select(a => a.Rect.Left).DefaultIfEmpty(Playfield.Left).Min();
        float rightMost = _aliens.Where(a => a.Alive).Select(a => a.Rect.Right).DefaultIfEmpty(Playfield.Right).Max();

        float leftLimit = Playfield.Left + 4f;
        float rightLimit = Playfield.Right - 4f;

        // Clamp step to wall so we "snap" then reverse; no vertical change
        float dx = _direction * _hStep;
        if (_direction > 0 && rightMost + dx > rightLimit)
            dx = rightLimit - rightMost;
        else if (_direction < 0 && leftMost + dx < leftLimit)
            dx = leftLimit - leftMost;

        for (int i = 0; i < _aliens.Count; i++)
        {
            var a = _aliens[i];
            if (!a.Alive) continue;
            var r = a.Rect;
            r.X = Math.Clamp(r.X + dx, Playfield.Left + 2f, Playfield.Right - r.Width - 2f);
            _aliens[i] = a with { Rect = r };
        }

        // If we had to clamp (hit wall), reverse direction
        if (dx != _direction * _hStep)
            _direction *= -1;
    }

    private void UpdateShots(float dt)
    {
        // Move
        for (int i = 0; i < _shots.Count; i++)
        {
            var s = _shots[i];
            s = s with { Y = s.Y + s.Vy * dt };
            _shots[i] = s;
        }

        // Remove out-of-bounds
        _shots.RemoveAll(s => s.Y + s.H < Playfield.Top || s.Y > Playfield.Bottom);

        // Player shots vs aliens
        for (int i = _shots.Count - 1; i >= 0; i--)
        {
            var s = _shots[i];
            if (!s.FromPlayer) continue;

            var hitIdx = _aliens.FindIndex(a => a.Alive && RectsOverlap(new RectF(s.X, s.Y, s.W, s.H), a.Rect));
            if (hitIdx >= 0)
            {
                var a = _aliens[hitIdx];
                _aliens[hitIdx] = a with { Alive = false };
                _shots.RemoveAt(i);
                Score += 10 + (4 - a.Row) * 5; // higher rows worth more
                NotifyHud();
            }
        }

        // Enemy shots vs player
        var shipRect = ShipRect;
        for (int i = _shots.Count - 1; i >= 0; i--)
        {
            var s = _shots[i];
            if (s.FromPlayer) continue;
            if (RectsOverlap(new RectF(s.X, s.Y, s.W, s.H), shipRect))
            {
                _shots.RemoveAt(i);
                LoseLife();
                break;
            }
        }
    }

    private void LoseLife()
    {
        if (State != SIState.Running) return;

        Lives--;
        NotifyHud();

        if (Lives <= 0)
        {
            State = SIState.GameOver;
            _timer?.Stop();
            GameOverEvent?.Invoke(Score, Wave);
            return;
        }

        // Pause round: reset shots, reposition ship; aliens keep current positions
        _shots.Clear();
        _shipX = Playfield.Center.X;
        _playerFireCooldown = 0f;
        _enemyTimer = 0.4f;

        State = SIState.Paused;
        _timer?.Stop();
    }

    private void CheckWaveOrGameOver()
    {
        if (_aliens.Count > 0 && _aliens.All(a => !a.Alive))
        {
            Wave++;
            LayoutEntitiesForWave();
            // remain Running
        }
    }

    private static bool RectsOverlap(RectF a, RectF b) =>
        !(a.Right < b.Left || a.Left > b.Right || a.Bottom < b.Top || a.Top > b.Bottom);

    private void Invalidate() =>
        MainThread.BeginInvokeOnMainThread(() => _canvas?.Invalidate());

    private void NotifyHud()
    {
        PropertyChanged?.Invoke(this, new(nameof(Score)));
        PropertyChanged?.Invoke(this, new(nameof(Lives)));
        PropertyChanged?.Invoke(this, new(nameof(Wave)));
        PropertyChanged?.Invoke(this, new(nameof(State)));
    }

    // Simple Lerp (compat with older targets)
    private static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0f, 1f);

    // ==== Exposed to drawable ====
    public RectF ShipRect => new(_shipX - _shipW / 2f, _shipY - _shipH / 2f, _shipW, _shipH);
    public System.Collections.Generic.IReadOnlyList<(RectF rect, bool alive, int row)> Aliens =>
        _aliens.Select(a => (a.Rect, a.Alive, a.Row)).ToList();
    public System.Collections.Generic.IReadOnlyList<(RectF rect, bool fromPlayer)> Shots =>
        _shots.Select(s => (new RectF(s.X, s.Y, s.W, s.H), s.FromPlayer)).ToList();
}
