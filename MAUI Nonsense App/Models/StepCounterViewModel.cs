using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Timers;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Models
{
    public class StepDay
    {
        public string Date { get; set; }
        public int Steps { get; set; }
    }

    public class StepCounterViewModel : INotifyPropertyChanged
    {
        private readonly IStepCounterService _service;
        private readonly System.Timers.Timer _refreshTimer;

        public event PropertyChangedEventHandler PropertyChanged;

        private int _todaySteps;
        public int TodaySteps
        {
            get => _todaySteps;
            set { if (_todaySteps != value) { _todaySteps = value; OnPropertyChanged(nameof(TodaySteps)); } }
        }

        private int _accumulatedSteps;
        public int AccumulatedSteps
        {
            get => _accumulatedSteps;
            set { if (_accumulatedSteps != value) { _accumulatedSteps = value; OnPropertyChanged(nameof(AccumulatedSteps)); } }
        }

        private int _rawSensorValue;
        public int RawSensorValue
        {
            get => _rawSensorValue;
            set { if (_rawSensorValue != value) { _rawSensorValue = value; OnPropertyChanged(nameof(RawSensorValue)); } }
        }

        public ObservableCollection<StepDay> Last7Days { get; set; } = new();

        public StepCounterViewModel(IStepCounterService service)
        {
            _service = service;

            _refreshTimer = new System.Timers.Timer(5000);
            _refreshTimer.Elapsed += async (s, e) => await RefreshAsync();
            _refreshTimer.AutoReset = true;

            _refreshTimer.Start();
            _ = RefreshAsync();
        }

        public void StartTimer() => _refreshTimer.Start();
        public void StopTimer() => _refreshTimer.Stop();

        public async Task RefreshAsync()
        {
#if IOS
            if (_service is MAUI_Nonsense_App.Platforms.iOS.Services.StepCounter.iOSStepCounterService iosService)
            {
                await iosService.FetchCurrentStepsAsync();
            }
#endif
            int current = _service.TotalSteps;

            TodaySteps = current;
            RawSensorValue = _service.RawSensorValue;

            var history = _service.StepHistory;
            string today = DateTime.Now.Date.ToString("yyyy-MM-dd"); // LOCAL TIME

            int historicalTotal = history
                .Where(kv => kv.Key != today)
                .Sum(kv => kv.Value);

            AccumulatedSteps = historicalTotal + TodaySteps;

            LoadLast7Days();
        }

        private void LoadLast7Days()
        {
            Last7Days.Clear();
            var history = _service.StepHistory;
            string today = DateTime.Now.Date.ToString("yyyy-MM-dd"); // LOCAL TIME

            for (int i = 0; i < 7; i++)
            {
                var dateObj = DateTime.Now.Date.AddDays(-i); // LOCAL TIME
                var date = dateObj.ToString("yyyy-MM-dd");

                int steps;
                if (date == today)
                {
                    steps = _service.TotalSteps;
                }
                else
                {
                    history.TryGetValue(date, out steps);
                }

                Last7Days.Add(new StepDay
                {
                    Date = date,
                    Steps = steps
                });
            }
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
