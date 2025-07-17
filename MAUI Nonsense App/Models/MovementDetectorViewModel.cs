using MAUI_Nonsense_App.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MAUI_Nonsense_App.Models
{
    public class MovementDetectorViewModel : INotifyPropertyChanged
    {
        private readonly IMovementAlarmService _movementAlarmService;
        private readonly IAlarmSoundService _alarmSoundService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<int> AvailableDelays { get; } =
            new ObservableCollection<int> { 5, 10, 30, 60 };

        public ObservableCollection<string> AvailableSensitivities { get; } =
            new ObservableCollection<string> { "Low", "Medium", "High" };

        private int _selectedDelay = 10;
        public int SelectedDelay
        {
            get => _selectedDelay;
            set { if (_selectedDelay != value) { _selectedDelay = value; OnPropertyChanged(); } }
        }

        private string _selectedSensitivity = "Medium";
        public string SelectedSensitivity
        {
            get => _selectedSensitivity;
            set { if (_selectedSensitivity != value) { _selectedSensitivity = value; OnPropertyChanged(); } }
        }

        private string _statusMessage = "Alarm is disarmed.";
        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
        }

        public string ArmButtonText => _isArmed ? "Disarm Alarm" : "Arm Alarm";

        public bool ArmButtonEnabled
        {
            get => _armButtonEnabled;
            private set { if (_armButtonEnabled != value) { _armButtonEnabled = value; OnPropertyChanged(); } }
        }

        public ICommand ToggleAlarmCommand { get; }

        private bool _isArmed;
        private bool _armButtonEnabled = true;

        public MovementDetectorViewModel(
            IMovementAlarmService movementAlarmService,
            IAlarmSoundService alarmSoundService)
        {
            _movementAlarmService = movementAlarmService;
            _alarmSoundService = alarmSoundService;

            ToggleAlarmCommand = new Command(async () => await ToggleAlarmAsync());
            _movementAlarmService.MovementDetected += OnMovementDetected;
        }

        private async Task ToggleAlarmAsync()
        {
            if (_isArmed)
            {
                await _movementAlarmService.StopAsync();
                _isArmed = false;
                StatusMessage = "Alarm is disarmed.";
            }
            else
            {
                int sensitivityValue = SelectedSensitivity switch
                {
                    "Low" => 1,
                    "Medium" => 2,
                    "High" => 3,
                    _ => 2
                };

                _movementAlarmService.Configure(SelectedDelay, sensitivityValue);
                await _movementAlarmService.StartAsync();

                ArmButtonEnabled = false; // disable button during countdown
                await RunCountdownAsync(SelectedDelay);

                _isArmed = true;
                StatusMessage = "Alarm armed and monitoring…";
                ArmButtonEnabled = true;
            }

            OnPropertyChanged(nameof(ArmButtonText));
        }

        private bool _countdownActive = false;

        public bool IsArmButtonEnabled => !_countdownActive;

        private async Task RunCountdownAsync(int seconds)
        {
            _countdownActive = true;
            OnPropertyChanged(nameof(IsArmButtonEnabled));

            for (int i = seconds; i > 0; i--)
            {
                StatusMessage = $"Arming in {i}…";
                await Task.Delay(1000);
            }

            _countdownActive = false;
            OnPropertyChanged(nameof(IsArmButtonEnabled));
        }

        private bool _alarmActive = false;

        private CancellationTokenSource? _alarmCts;

        private void OnMovementDetected(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = "⚠️ Movement detected!";
            });

            _alarmCts?.Cancel();
            _alarmCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(10_000, _alarmCts.Token);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StatusMessage = "Alarm armed and monitoring…";
                    });
                }
                catch (TaskCanceledException)
                {
                    // ignored — another motion reset the timer
                }
            });
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
