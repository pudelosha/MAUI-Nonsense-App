using System.ComponentModel;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.ViewModels;

public enum TetrisState { Ready, Running, Paused, GameOver }

public class TetrisViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<int>? GameOverEvent;

    public int Score { get; private set; }
    public int Level { get; private set; } = 1;
    public TetrisState State { get; private set; } = TetrisState.Ready;

    public int Rows => 20;
    public int Cols => 10;
    public int CellPx { get; private set; } = 20;

    private int[,]? _board; // null until Start()
    private (int x, int y)[] _currentShape = Array.Empty<(int, int)>();
    private int _currentX, _currentY, _currentColor;
    private readonly Random _rng = new();

    private GraphicsView? _canvas;
    private IDispatcherTimer? _timer;

    public void SetCanvasSize(Size size)
    {
        // Fit the entire 10x20 board inside the playfield bounds (no clipping).
        var byWidth = size.Width / Cols;
        var byHeight = size.Height / Rows;

        CellPx = (int)Math.Floor(Math.Min(byWidth, byHeight));
        if (CellPx < 1) CellPx = 1;
    }

    public void Start(GraphicsView canvas)
    {
        _canvas = canvas;
        if (State == TetrisState.Ready || State == TetrisState.GameOver)
            InitGame();

        State = TetrisState.Running;
        EnsureTimer();
        _timer!.Start();
    }

    public void Pause()
    {
        _timer?.Stop();
        if (State == TetrisState.Running)
            State = TetrisState.Paused;
    }

    public void Reset()
    {
        _timer?.Stop();
        _board = null;   // nothing draws until Start again
        State = TetrisState.Ready;
        Score = 0;
        Level = 1;
        OnHudChanged();
        Invalidate();
    }

    private void InitGame()
    {
        Score = 0;
        Level = 1;
        _board = new int[Rows, Cols];
        SpawnShape();
        Invalidate();
        OnHudChanged();
    }

    private void EnsureTimer()
    {
        _timer ??= Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(500 / Math.Max(1, Level));
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
    }

    private void OnTick(object? sender, EventArgs e) => Step();

    private void Step()
    {
        if (State != TetrisState.Running) return;

        if (!Move(0, 1)) // try move down
        {
            PlaceShape();
            ClearLines();
            SpawnShape();
            if (!ValidPosition(_currentShape, _currentX, _currentY))
            {
                GameOver();
                return;
            }
        }
        Invalidate();
    }

    public void MoveLeft() { if (State == TetrisState.Running) Move(-1, 0); }
    public void MoveRight() { if (State == TetrisState.Running) Move(1, 0); }
    public void Rotate()
    {
        if (State != TetrisState.Running) return;
        var rotated = _currentShape.Select(p => (-p.y, p.x)).ToArray();
        if (ValidPosition(rotated, _currentX, _currentY))
            _currentShape = rotated;
        Invalidate();
    }
    public void Drop()
    {
        if (State != TetrisState.Running) return;
        // Move until we can't; Move() returns false when no change
        while (Move(0, 1)) { }
        Step(); // lock, clear, spawn
    }

    private bool Move(int dx, int dy)
    {
        if (_board == null) return false;

        int nx = _currentX + dx;
        int ny = _currentY + dy;

        if (!ValidPosition(_currentShape, nx, ny))
            return false;

        if (nx == _currentX && ny == _currentY)
            return false; // no actual movement (safety for Drop)

        _currentX = nx;
        _currentY = ny;
        Invalidate();
        return true;
    }

    private void PlaceShape()
    {
        if (_board == null) return;
        foreach (var (x, y) in _currentShape)
        {
            var bx = _currentX + x;
            var by = _currentY + y;
            if (by >= 0 && by < Rows && bx >= 0 && bx < Cols)
                _board[by, bx] = _currentColor;
        }
    }

    private void ClearLines()
    {
        if (_board == null) return;
        int lines = 0;
        for (int y = Rows - 1; y >= 0; y--)
        {
            bool full = true;
            for (int x = 0; x < Cols; x++)
                if (_board[y, x] == 0) { full = false; break; }
            if (full)
            {
                lines++;
                for (int yy = y; yy > 0; yy--)
                    for (int xx = 0; xx < Cols; xx++)
                        _board[yy, xx] = _board[yy - 1, xx];
                for (int xx = 0; xx < Cols; xx++) _board[0, xx] = 0;
                y++;
            }
        }
        if (lines > 0)
        {
            Score += lines switch { 1 => 40, 2 => 100, 3 => 300, 4 => 1200, _ => 0 } * Level;
            Level = 1 + Score / 1000;
            EnsureTimer();
            OnHudChanged();
        }
    }

    private void SpawnShape()
    {
        var shapes = new[]
        {
            new[]{ (0,0),(1,0),(-1,0),(-2,0)}, // I
            new[]{ (0,0),(1,0),(0,1),(1,1)},   // O
            new[]{ (0,0),(-1,0),(1,0),(1,1)},  // J
            new[]{ (0,0),(1,0),(-1,0),(-1,1)}, // L
            new[]{ (0,0),(1,0),(0,1),(-1,1)},  // S
            new[]{ (0,0),(-1,0),(0,1),(1,1)},  // Z
            new[]{ (0,0),(-1,0),(1,0),(0,1)}   // T
        };

        _currentShape = shapes[_rng.Next(shapes.Length)];
        _currentColor = _rng.Next(1, 8);

        // center horizontally by shape bounds
        int minX = _currentShape.Min(p => p.x);
        int maxX = _currentShape.Max(p => p.x);
        int shapeWidth = maxX - minX + 1;

        _currentX = (Cols - shapeWidth) / 2 - minX;
        _currentY = 0;
    }

    private bool ValidPosition((int x, int y)[] shape, int posX, int posY)
    {
        if (_board == null) return false;
        foreach (var (x, y) in shape)
        {
            int nx = posX + x;
            int ny = posY + y;
            if (nx < 0 || nx >= Cols || ny >= Rows) return false;
            if (ny >= 0 && _board[ny, nx] != 0) return false;
        }
        return true;
    }

    private void GameOver()
    {
        State = TetrisState.GameOver;
        _timer?.Stop();
        GameOverEvent?.Invoke(Score);
    }

    public int[,]? Board => _board;
    public (int x, int y)[] CurrentShape => _currentShape;
    public int CurrentX => _currentX;
    public int CurrentY => _currentY;
    public int CurrentColor => _currentColor;

    private void Invalidate() => MainThread.BeginInvokeOnMainThread(() => _canvas?.Invalidate());

    private void OnHudChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Score)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Level)));
    }
}
