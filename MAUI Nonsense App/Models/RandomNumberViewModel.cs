using System.ComponentModel;
using System.Windows.Input;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Models;

public class RandomNumberViewModel : INotifyPropertyChanged
{
    private readonly IRandomNumberService _randomService;

    public event PropertyChangedEventHandler? PropertyChanged;

    int _from = 1;
    int _to = 100;
    int _count = 1;
    bool _allowDuplicates = false;
    string _result = string.Empty;

    public int From
    {
        get => _from;
        set { _from = value; OnPropertyChanged(nameof(From)); }
    }

    public int To
    {
        get => _to;
        set { _to = value; OnPropertyChanged(nameof(To)); }
    }

    public int Count
    {
        get => _count;
        set
        {
            _count = value;
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(CanAllowDuplicates));
        }
    }

    public bool AllowDuplicates
    {
        get => _allowDuplicates;
        set { _allowDuplicates = value; OnPropertyChanged(nameof(AllowDuplicates)); }
    }

    public bool CanAllowDuplicates => Count > 1;

    public string Result
    {
        get => _result;
        set { _result = value; OnPropertyChanged(nameof(Result)); }
    }

    public ICommand GenerateCommand { get; }

    public RandomNumberViewModel(IRandomNumberService randomService)
    {
        _randomService = randomService;
        GenerateCommand = new Command(Generate);
    }

    public void Generate()
    {
        try
        {
            var numbers = _randomService.GenerateNumbers(From, To, Count, AllowDuplicates);
            numbers.Sort(); // sort ascending
            Result = string.Join(", ", numbers);
        }
        catch (Exception ex)
        {
            Result = $"Error: {ex.Message}";
        }
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
