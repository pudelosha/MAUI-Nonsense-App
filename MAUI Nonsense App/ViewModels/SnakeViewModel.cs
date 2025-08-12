using System.ComponentModel;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.ViewModels;

public enum GameState { Ready, Running, Paused, GameOver }
public enum Direction { Up, Right, Down, Left }

public class SnakeViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // Game over callback (emits final score)
    public event Action<int>? GameOverEvent;

    // Public HUD
    public int Score { get; private set; }
    public GameState State { get; private set; } = GameState.Ready;
    public double SpeedMultiplier { get; private set; } = 1.0;

    // Grid setup
    private const int CellSize = 20;   // px per cell
    private int _cols, _rows;
    private Size _canvasSize;

    // Snake + Fruit
    private readonly LinkedList<Point> _snake = new(); // grid coords
    private Direction _dir = Direction.Right;
    private Point _fruit;
    private readonly Random _rng = new();

    // Timing (game loop)
    private IDispatcherTimer? _timer;
    private readonly TimeSpan _baseInterval = TimeSpan.FromMilliseconds(160); // ~6.25 steps/s base

    // Rendering invalidation target
    private GraphicsView? _canvas;

    public void SetCanvasSize(Size size)
    {
        _canvasSize = size;
        _cols = Math.Max(8, (int)Math.Floor(size.Width / CellSize));
        _rows = Math.Max(8, (int)Math.Floor(size.Height / CellSize));
        if (State == GameState.Ready || State == GameState.GameOver)
        {
            InitGame();
        }
    }

    public void Start(GraphicsView canvas)
    {
        if (_cols == 0 || _rows == 0) return; // not ready
        _canvas = canvas;

        if (State == GameState.Ready || State == GameState.GameOver)
            InitGame();

        State = GameState.Running;
        EnsureTimer();
        _timer!.Start();
        OnHudChanged();
    }

    public void Pause()
    {
        _timer?.Stop();
        if (State == GameState.Running)
            State = GameState.Paused;
        OnHudChanged();
    }

    public void Reset()
    {
        _timer?.Stop();
        InitGame();
        State = GameState.Ready;
        OnHudChanged();
    }

    public void TurnLeft()
    {
        if (State != GameState.Running) return;
        _dir = _dir switch
        {
            Direction.Up => Direction.Left,
            Direction.Left => Direction.Down,
            Direction.Down => Direction.Right,
            Direction.Right => Direction.Up,
            _ => _dir
        };
    }

    public void TurnRight()
    {
        if (State != GameState.Running) return;
        _dir = _dir switch
        {
            Direction.Up => Direction.Right,
            Direction.Right => Direction.Down,
            Direction.Down => Direction.Left,
            Direction.Left => Direction.Up,
            _ => _dir
        };
    }

    private void InitGame()
    {
        Score = 0;
        SpeedMultiplier = 1.0;
        _snake.Clear();

        // Centered 4-segment snake, heading right
        var cx = _cols / 2;
        var cy = _rows / 2;
        _dir = Direction.Right;

        for (int i = 3; i >= 0; i--)
            _snake.AddLast(new Point(cx - i, cy));

        SpawnFruit();
        Invalidate();
    }

    private void EnsureTimer()
    {
        _timer ??= Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = EffectiveInterval();
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
    }

    private TimeSpan EffectiveInterval()
    {
        var ms = _baseInterval.TotalMilliseconds / SpeedMultiplier;
        return TimeSpan.FromMilliseconds(Math.Max(55, ms)); // clamp
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (State != GameState.Running) return;

        // Move head
        var head = _snake.Last!.Value;
        var next = NextCell(head, _dir);

        // Wall collision
        if (next.X < 0 || next.X >= _cols || next.Y < 0 || next.Y >= _rows)
        {
            GameOver();
            return;
        }

        // Self collision (ignore tail cell only if we will move it — i.e., not eating)
        var eating = next == _fruit;
        if (ContainsCell(next) && !(IsTail(next) && !eating))
        {
            GameOver();
            return;
        }

        _snake.AddLast(next);

        if (eating)
        {
            Score += 1;
            SpeedMultiplier = Math.Min(2.2, 1.0 + Score * 0.06); // gentle ramp
            _timer!.Interval = EffectiveInterval();
            SpawnFruit();
        }
        else
        {
            _snake.RemoveFirst(); // move tail forward
        }

        Invalidate();
        OnHudChanged();
    }

    private void GameOver()
    {
        State = GameState.GameOver;
        _timer?.Stop();
        OnHudChanged();
        GameOverEvent?.Invoke(Score); // notify page to show popup
    }

    private Point NextCell(Point p, Direction d) => d switch
    {
        Direction.Up => new Point(p.X, p.Y - 1),
        Direction.Down => new Point(p.X, p.Y + 1),
        Direction.Left => new Point(p.X - 1, p.Y),
        Direction.Right => new Point(p.X + 1, p.Y),
        _ => p
    };

    private bool ContainsCell(Point cell) => _snake.Any(s => s == cell);
    private bool IsTail(Point cell) => _snake.First!.Value == cell;

    private void SpawnFruit()
    {
        if (_cols * _rows <= _snake.Count)
        {
            // filled board — you win (treat as game over)
            GameOver();
            return;
        }

        Point p;
        do
        {
            p = new Point(_rng.Next(0, _cols), _rng.Next(0, _rows));
        } while (ContainsCell(p));
        _fruit = p;
    }

    public IReadOnlyCollection<Point> SnakeCells => _snake;
    public Point FruitCell => _fruit;
    public int Cols => _cols;
    public int Rows => _rows;
    public int CellPx => CellSize;
    public Direction Heading => _dir;

    private void Invalidate()
    {
        MainThread.BeginInvokeOnMainThread(() => _canvas?.Invalidate());
    }

    private void OnHudChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Score)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpeedMultiplier)));
    }
}
