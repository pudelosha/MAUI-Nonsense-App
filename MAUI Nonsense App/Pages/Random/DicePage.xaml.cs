using System.Timers;
using Microsoft.Maui.Controls.Shapes;

namespace MAUI_Nonsense_App.Pages.Random;

public partial class DicePage : ContentPage
{
    private readonly System.Random _random = new();
    private int _diceCount = 1;
    private readonly AbsoluteLayout _diceArea;
    private readonly Label _resultLabel;
    private readonly List<DiceView> _diceViews = new();
    private readonly Button _rollButton;

    public DicePage()
    {
        Title = "Dice";

        _diceArea = new AbsoluteLayout
        {
            HeightRequest = 400,
            BackgroundColor = Colors.WhiteSmoke
        };

        _rollButton = new Button
        {
            Text = "Roll Dices",
            Margin = new Thickness(0, 10),
            HorizontalOptions = LayoutOptions.Center
        };
        _rollButton.Clicked += OnRollClicked;

        var decreaseButton = new Button { Text = "-", WidthRequest = 50 };
        decreaseButton.Clicked += (s, e) => ChangeDiceCount(-1);

        var increaseButton = new Button { Text = "+", WidthRequest = 50 };
        increaseButton.Clicked += (s, e) => ChangeDiceCount(1);

        var countLayout = new HorizontalStackLayout
        {
            Spacing = 20,
            HorizontalOptions = LayoutOptions.Center,
            Children = { decreaseButton, increaseButton }
        };

        _resultLabel = new Label
        {
            Text = "",
            FontSize = 20,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 10)
        };

        var mainLayout = new VerticalStackLayout
        {
            Children = { _diceArea, countLayout, _rollButton, _resultLabel }
        };

        Content = new ScrollView { Content = mainLayout };

        BuildDices();
    }

    private void ChangeDiceCount(int delta)
    {
        int newCount = Math.Clamp(_diceCount + delta, 1, 3);
        if (newCount != _diceCount)
        {
            _diceCount = newCount;
            BuildDices();
        }
    }

    private void BuildDices()
    {
        _diceArea.Children.Clear();
        _diceViews.Clear();

        for (int i = 0; i < _diceCount; i++)
        {
            var dice = new DiceView();
            AbsoluteLayout.SetLayoutBounds(dice, new Rect(100 + i * 100, 100, 80, 80));
            _diceArea.Children.Add(dice);
            _diceViews.Add(dice);
        }
    }

    private async void OnRollClicked(object sender, EventArgs e)
    {
        foreach (var dice in _diceViews)
            dice.StartRolling();

        await Task.Delay(2000);

        int total = 0;
        foreach (var dice in _diceViews)
        {
            int value = _random.Next(1, 7);
            dice.StopRolling(value);
            total += value;
        }

        _resultLabel.Text = $"Results: {string.Join(", ", _diceViews.Select(d => d.CurrentValue))} | Total: {total}";
    }
}

public class DiceView : GraphicsView
{
    private readonly System.Random _random = new();
    private bool _rolling = false;
    private int _currentValue = 1;
    public int CurrentValue => _currentValue;

    public DiceView()
    {
        Drawable = new DiceDrawable(() => _currentValue);
        BackgroundColor = Colors.White;
    }

    public void StartRolling()
    {
        _rolling = true;
        Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
        {
            if (!_rolling) return false;
            _currentValue = _random.Next(1, 7);
            Invalidate();
            return true;
        });
    }

    public void StopRolling(int value)
    {
        _rolling = false;
        _currentValue = value;
        Invalidate();
    }
}

public class DiceDrawable : IDrawable
{
    private readonly Func<int> _getValue;

    public DiceDrawable(Func<int> getValue)
    {
        _getValue = getValue;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.White;
        canvas.FillRoundedRectangle(dirtyRect, 10);

        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 2;
        canvas.DrawRoundedRectangle(dirtyRect, 10);

        canvas.FillColor = Colors.Black;
        float w = dirtyRect.Width;
        float h = dirtyRect.Height;

        void Dot(float x, float y)
        {
            canvas.FillCircle(dirtyRect.X + x * w, dirtyRect.Y + y * h, w * 0.07f);
        }

        int v = _getValue();
        if (v == 1 || v == 3 || v == 5) Dot(0.5f, 0.5f);
        if (v >= 2) { Dot(0.25f, 0.25f); Dot(0.75f, 0.75f); }
        if (v >= 4) { Dot(0.25f, 0.75f); Dot(0.75f, 0.25f); }
        if (v == 6) { Dot(0.25f, 0.5f); Dot(0.75f, 0.5f); }
    }
}
