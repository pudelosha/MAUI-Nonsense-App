using System.ComponentModel;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Models
{
    public class StepCounterViewModel : INotifyPropertyChanged
    {
        private readonly IStepCounterService _service;

        public event PropertyChangedEventHandler PropertyChanged;

        private int _totalSteps;
        public int TotalSteps
        {
            get => _totalSteps;
            set
            {
                if (_totalSteps != value)
                {
                    _totalSteps = value;
                    OnPropertyChanged(nameof(TotalSteps));
                }
            }
        }

        private int _last24HoursSteps;
        public int Last24HoursSteps
        {
            get => _last24HoursSteps;
            set
            {
                if (_last24HoursSteps != value)
                {
                    _last24HoursSteps = value;
                    OnPropertyChanged(nameof(Last24HoursSteps));
                }
            }
        }

        public StepCounterViewModel(IStepCounterService service)
        {
            _service = service;

            // initialize with current values
            TotalSteps = service.TotalSteps;
            Last24HoursSteps = service.Last24HoursSteps;

            service.StepsUpdated += (s, e) =>
            {
                TotalSteps = service.TotalSteps;
                Last24HoursSteps = service.Last24HoursSteps;
            };
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
