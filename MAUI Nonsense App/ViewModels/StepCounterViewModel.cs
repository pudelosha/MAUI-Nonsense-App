using System.Collections.ObjectModel;
using System.ComponentModel;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.ViewModels
{
    public class StepDay
    {
        public string Date { get; set; }
        public int Steps { get; set; }
    }

    public class StepCounterViewModel : INotifyPropertyChanged
    {
        private readonly IStepCounterService _service;

        public event PropertyChangedEventHandler PropertyChanged;

        public int TotalSteps
        {
            get => _totalSteps;
            set { if (_totalSteps != value) { _totalSteps = value; OnPropertyChanged(nameof(TotalSteps)); } }
        }
        private int _totalSteps;

        public int Last24HoursSteps
        {
            get => _last24HoursSteps;
            set { if (_last24HoursSteps != value) { _last24HoursSteps = value; OnPropertyChanged(nameof(Last24HoursSteps)); } }
        }
        private int _last24HoursSteps;

        public ObservableCollection<StepDay> Last7Days { get; set; } = new();

        public StepCounterViewModel(IStepCounterService service)
        {
            _service = service;

            Console.WriteLine($"[VM] Initial TotalSteps: {_service.TotalSteps}");
            Console.WriteLine($"[VM] Initial DailySteps: {_service.Last24HoursSteps}");

            TotalSteps = _service.TotalSteps;
            Last24HoursSteps = _service.Last24HoursSteps;
            LoadLast7Days();

            _service.StepsUpdated += (s, e) =>
            {
                Console.WriteLine($"[VM] StepsUpdated: Total={_service.TotalSteps}, Daily={_service.Last24HoursSteps}");

                TotalSteps = _service.TotalSteps;
                Last24HoursSteps = _service.Last24HoursSteps;
                LoadLast7Days();
            };
        }

        private void LoadLast7Days()
        {
            Last7Days.Clear();
            var history = _service.StepHistory;

            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i).ToString("yyyy-MM-dd");
                history.TryGetValue(date, out int steps);

                Last7Days.Add(new StepDay
                {
                    Date = date,
                    Steps = steps
                });
            }
        }

        public void ReloadLast7Days() => LoadLast7Days();

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
