using System.ComponentModel;
using Microsoft.Maui.Graphics;

namespace MAUI_Nonsense_App.ViewModels;

public enum Game2048State { Ready, Running, GameOver }
public enum MoveDir { Left, Right, Up, Down }

public class Game2048ViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<int>? GameOverEvent;

    public int Score { get; private set; }
    public int Best { get; private set; }
    public Game2048State State { get; private set; } = Game2048State.Ready;

    public int Size => 4;            // 4x4
    public float Gap => 8f;          // px between tiles
    public RectF Playfield { get; private set; } // inner board rect (light gray)
    public float CellPx { get; private set; }    // tile width/height in px

    private readonly Random _rng = new();
    private int[,]? _board;          // null until Start()

    private GraphicsView? _canvas;

    public void SetCanvasSize(Size size)
    {
        // Center a square board with small outer margin
        float margin = 8f;
        float usableW = (float)size.Width - 2 * margin;
        float usableH = (float)size.Height - 2 * margin;
        float board = MathF.Min(usableW, usableH);

        // Cell size considering gaps (4 cells => 5 gaps)
        CellPx = MathF.Floor((board - (Size + 1) * Gap) / Size);
        if (CellPx < 10) CellPx = 10;

        float boardW = Size * CellPx + (Size + 1) * Gap;
        float boardH = Size * CellPx + (Size + 1) * Gap;

        float ox = ((float)size.Width - boardW) / 2f;
        float oy = ((float)size.Height - boardH) / 2f;

        Playfield = new RectF(ox, oy, boardW, boardH);

        if (State == Game2048State.Running)
            Invalidate();
    }

    public void Ready()
    {
        State = Game2048State.Ready;
        Score = 0;
        _board = null;
        NotifyHud();
        Invalidate();
    }

    public void Start(GraphicsView canvas)
    {
        _canvas = canvas;
        Score = 0;
        if (Best < 0) Best = 0;
        _board = new int[Size, Size];
        State = Game2048State.Running;

        Spawn();
        Spawn();
        NotifyHud();
        Invalidate();
    }

    public void Move(MoveDir dir)
    {
        if (_board == null)
        {
            // auto-start on first swipe/button press
            if (_canvas != null) Start(_canvas);
            else return;
        }
        if (State != Game2048State.Running) return;

        bool moved = dir switch
        {
            MoveDir.Left => SlideRows(left: true),
            MoveDir.Right => SlideRows(left: false),
            MoveDir.Up => SlideCols(up: true),
            MoveDir.Down => SlideCols(up: false),
            _ => false
        };

        if (!moved) return;

        Spawn();
        Invalidate();
        CheckGameOver();
        NotifyHud();
    }

    private bool SlideRows(bool left)
    {
        bool moved = false;
        for (int r = 0; r < Size; r++)
        {
            var line = new int[Size];
            for (int c = 0; c < Size; c++) line[c] = _board![r, c];

            if (!left) Array.Reverse(line);

            var (merged, gained) = MergeLine(line);
            if (!left) Array.Reverse(merged);

            for (int c = 0; c < Size; c++)
            {
                if (_board[r, c] != merged[c]) { moved = true; }
                _board[r, c] = merged[c];
            }

            if (gained > 0) Score += gained;
        }
        Best = Math.Max(Best, Score);
        return moved;
    }

    private bool SlideCols(bool up)
    {
        bool moved = false;
        for (int c = 0; c < Size; c++)
        {
            var line = new int[Size];
            for (int r = 0; r < Size; r++) line[r] = _board![r, c];

            if (!up) Array.Reverse(line);

            var (merged, gained) = MergeLine(line);
            if (!up) Array.Reverse(merged);

            for (int r = 0; r < Size; r++)
            {
                if (_board[r, c] != merged[r]) { moved = true; }
                _board[r, c] = merged[r];
            }

            if (gained > 0) Score += gained;
        }
        Best = Math.Max(Best, Score);
        return moved;
    }

    // Compacts to the front, merges equal neighbors once, then compacts again
    private (int[] line, int gained) MergeLine(int[] src)
    {
        var compact = src.Where(v => v != 0).ToList();
        var outLine = new List<int>(Size);
        int gained = 0;

        int i = 0;
        while (i < compact.Count)
        {
            if (i + 1 < compact.Count && compact[i] == compact[i + 1])
            {
                int val = compact[i] * 2;
                outLine.Add(val);
                gained += val;
                i += 2;
            }
            else
            {
                outLine.Add(compact[i]);
                i += 1;
            }
        }

        while (outLine.Count < Size) outLine.Add(0);
        return (outLine.ToArray(), gained);
    }

    private void Spawn()
    {
        if (_board == null) return;
        var empties = new List<(int r, int c)>();
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                if (_board[r, c] == 0) empties.Add((r, c));

        if (empties.Count == 0) return;

        var (er, ec) = empties[_rng.Next(empties.Count)];
        _board[er, ec] = _rng.NextDouble() < 0.9 ? 2 : 4;
    }

    private void CheckGameOver()
    {
        if (_board == null) return;

        // any empty?
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                if (_board[r, c] == 0) return;

        // any merge possible?
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                int v = _board[r, c];
                if (r + 1 < Size && _board[r + 1, c] == v) return;
                if (c + 1 < Size && _board[r, c + 1] == v) return;
            }

        State = Game2048State.GameOver;
        GameOverEvent?.Invoke(Score);
    }

    // Expose board for drawing (null when not started)
    public int[,]? Board => _board;

    private void Invalidate() =>
        MainThread.BeginInvokeOnMainThread(() => _canvas?.Invalidate());

    private void NotifyHud()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Score)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Best)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
    }
}
