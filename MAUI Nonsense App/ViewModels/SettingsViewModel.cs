using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Input;

namespace MAUI_Nonsense_App.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        // ---- Preferences keys
        private const string KeyBirthDate = "Settings.BirthDate";
        private const string KeySex = "Settings.Sex";
        private const string KeyHeightCm = "Settings.HeightCm";
        private const string KeyWeightKg = "Settings.WeightKg";
        private const string KeyStrideLengthCm = "Settings.StrideLengthCm";
        private const string KeyManualStrideOverride = "Settings.ManualStrideOverride";
        private const string KeyDailyGoal = "Settings.DailyGoal";
        private const string KeyReminderTime = "Settings.ReminderTime"; // HH:mm
        private const string KeyUnits = "Settings.Units";           // "Metric (cm, kg)" | "Imperial (ft, lb)"
        private const string KeyWeekStart = "Settings.WeekStart";   // "Monday" | "Sunday"
        private const string KeyLanguage = "Settings.Language";     // "English" | "Polish"
        private const string KeyDateFormat = "Settings.DateFormat"; // "yyyy-MM-dd" etc.

        // ---- Options (bind to Pickers)
        public IReadOnlyList<string> SexOptions { get; } = new List<string> { "", "Male", "Female" };
        public IReadOnlyList<string> UnitsOptions { get; } = new List<string> { "Metric (cm, kg)", "Imperial (ft, lb)" };
        public IReadOnlyList<string> WeekStartOptions { get; } = new List<string> { "Monday", "Sunday" };
        public IReadOnlyList<string> LanguageOptions { get; } = new List<string> { "English", "Polish" };
        public IReadOnlyList<string> DateFormatOptions { get; } = new List<string> { "yyyy-MM-dd", "dd.MM.yyyy", "MM/dd/yyyy" };

        // ---- Backing fields (ALWAYS METRIC internally)
        private DateTime _birthDate;
        private string _sex;
        private double? _heightCm;      // stored in cm (rounded 0.1)
        private double? _weightKg;      // stored in kg (rounded 0.1)
        private int? _strideLengthCm;   // stored in cm (int)
        private bool _manualStrideOverride;

        private int _dailyGoal;
        private TimeSpan _reminderTime;

        private string _units;
        private string _weekStart;
        private string _language;
        private string _dateFormat;

        // Imperial height text fields
        private string _heightFeetText = "";
        private string _heightInchesText = "";

        private readonly IAsyncRelayCommand _saveCommand;

        public event PropertyChangedEventHandler PropertyChanged;

        public SettingsViewModel()
        {
            var defaultBirth = new DateTime(2000, 1, 1);
            _birthDate = SafeGetDateTime(KeyBirthDate, defaultBirth);
            _sex = SafeGetString(KeySex, string.Empty);
            _heightCm = SafeGetNullableDouble(KeyHeightCm);
            _weightKg = SafeGetNullableDouble(KeyWeightKg);
            _manualStrideOverride = SafeGetBool(KeyManualStrideOverride, false);
            _strideLengthCm = SafeGetNullableIntWithMigration(KeyStrideLengthCm);

            _dailyGoal = SafeGetInt(KeyDailyGoal, 5000);
            var reminder = SafeGetString(KeyReminderTime, "08:00");
            _reminderTime = TimeSpan.TryParse(reminder, out var t) ? t : new TimeSpan(8, 0, 0);

            _units = SafeGetString(KeyUnits, "Metric (cm, kg)");
            _weekStart = SafeGetString(KeyWeekStart, "Monday");
            _language = SafeGetString(KeyLanguage, "English");
            _dateFormat = SafeGetString(KeyDateFormat, "yyyy-MM-dd");

            RefreshImperialHeightTextFromCm();
            MaybeAutoComputeStride();
            _saveCommand = new AsyncRelayCommand(SaveAsync);
        }

        // ---- Derived flags & dynamic labels
        public bool IsImperial => _units.StartsWith("Imperial", StringComparison.OrdinalIgnoreCase);
        public string HeightLabel => IsImperial ? "Height (ft, in)" : "Height (cm)";
        public string WeightLabel => $"Weight ({(IsImperial ? "lb" : "kg")})";
        public string StrideLabel => $"Stride Length ({(IsImperial ? "in" : "cm")})";

        // ---- Properties

        public DateTime BirthDate
        {
            get => _birthDate;
            set
            {
                if (_birthDate != value)
                {
                    _birthDate = value;
                    _manualStrideOverride = false;         // Option A: re-enable auto on driver fields
                    OnChanged(nameof(BirthDate));
                    MaybeAutoComputeStride();
                }
            }
        }

        public string Sex
        {
            get => _sex;
            set
            {
                if (_sex != value)
                {
                    _sex = value;
                    _manualStrideOverride = false;         // Option A
                    OnChanged(nameof(Sex));
                    MaybeAutoComputeStride();
                }
            }
        }

        // Height: GET displays 1 decimal (ft or cm). SET accepts current unit, stores cm rounded to 0.1.
        public double? HeightCm
        {
            get
            {
                if (!_heightCm.HasValue) return null;
                var display = IsImperial ? _heightCm.Value / 30.48 : _heightCm.Value;
                return Round1(display);
            }
            set
            {
                var incoming = ParseDoubleFlexible(value);
                double? cm = null;

                if (incoming.HasValue)
                {
                    cm = IsImperial ? incoming.Value * 30.48 : incoming.Value;
                    cm = Round1(cm.Value); // store 0.1 cm
                }

                if (!NullableEquals(_heightCm, cm))
                {
                    _heightCm = cm;
                    _manualStrideOverride = false;         // Option A
                    RefreshImperialHeightTextFromCm();
                    OnChanged(nameof(HeightCm));
                    OnChanged(nameof(HeightFeetText));
                    OnChanged(nameof(HeightInchesText));
                    MaybeAutoComputeStride();
                }
            }
        }

        // Imperial height text boxes
        public string HeightFeetText
        {
            get => _heightFeetText;
            set
            {
                if (_heightFeetText != value)
                {
                    _heightFeetText = value ?? "";
                    RecomputeHeightFromImperialTexts();     // will also reset manual flag (Option A)
                    OnChanged(nameof(HeightFeetText));
                }
            }
        }

        public string HeightInchesText
        {
            get => _heightInchesText;
            set
            {
                if (_heightInchesText != value)
                {
                    _heightInchesText = value ?? "";
                    RecomputeHeightFromImperialTexts();     // will also reset manual flag (Option A)
                    OnChanged(nameof(HeightInchesText));
                }
            }
        }

        // Weight: GET displays 1 decimal (lb or kg). SET accepts current unit, stores kg rounded to 0.1.
        public double? WeightKg
        {
            get
            {
                if (!_weightKg.HasValue) return null;
                var display = IsImperial ? KgToLb(_weightKg.Value) : _weightKg.Value;
                return Round1(display);
            }
            set
            {
                var incoming = ParseDoubleFlexible(value);
                double? kg = null;

                if (incoming.HasValue)
                {
                    kg = IsImperial ? LbToKg(incoming.Value) : incoming.Value;
                    kg = Round1(kg.Value); // store 0.1 kg
                }

                if (!NullableEquals(_weightKg, kg))
                {
                    _weightKg = kg;
                    OnChanged(nameof(WeightKg));
                }
            }
        }

        // Stride: GET shows inches (imperial) or cm (metric). SET expects inches/ cm; stores cm int.
        public int? StrideLengthCm
        {
            get
            {
                if (!_strideLengthCm.HasValue) return null;
                if (IsImperial)
                {
                    var inches = _strideLengthCm.Value / 2.54;
                    return (int)Math.Round(inches, MidpointRounding.AwayFromZero);
                }
                return _strideLengthCm;
            }
            set
            {
                int? incoming = NormalizeInt(value);
                int? cm = null;

                if (incoming.HasValue)
                {
                    cm = IsImperial
                        ? (int)Math.Round(incoming.Value * 2.54, MidpointRounding.AwayFromZero)
                        : incoming.Value;
                }

                if (_strideLengthCm != cm)
                {
                    _strideLengthCm = cm;
                    _manualStrideOverride = cm.HasValue; // user typed => manual
                    OnChanged(nameof(StrideLengthCm));
                }
            }
        }

        public int DailyGoal
        {
            get => _dailyGoal;
            set { if (_dailyGoal != value) { _dailyGoal = Math.Max(0, value); OnChanged(nameof(DailyGoal)); } }
        }

        public TimeSpan ReminderTime
        {
            get => _reminderTime;
            set { if (_reminderTime != value) { _reminderTime = value; OnChanged(nameof(ReminderTime)); } }
        }

        public string Units
        {
            get => _units;
            set
            {
                if (_units != value && !string.IsNullOrWhiteSpace(value))
                {
                    var old = _units;
                    _units = value;
                    OnChanged(nameof(Units));

                    // Refresh labels and displayed values for the new unit
                    OnChanged(nameof(IsImperial));
                    OnChanged(nameof(HeightLabel));
                    OnChanged(nameof(WeightLabel));
                    OnChanged(nameof(StrideLabel));

                    // Update height presentation
                    RefreshImperialHeightTextFromCm();
                    OnChanged(nameof(HeightFeetText));
                    OnChanged(nameof(HeightInchesText));
                    OnChanged(nameof(HeightCm));
                    OnChanged(nameof(WeightKg));
                    OnChanged(nameof(StrideLengthCm));

                    HandleUnitsChange(old, _units);
                }
            }
        }

        public string WeekStart { get => _weekStart; set { if (_weekStart != value) { _weekStart = value; OnChanged(nameof(WeekStart)); } } }
        public string Language { get => _language; set { if (_language != value) { _language = value; OnChanged(nameof(Language)); } } }
        public string DateFormat { get => _dateFormat; set { if (_dateFormat != value) { _dateFormat = value; OnChanged(nameof(DateFormat)); } } }

        // ---- Commands
        public IAsyncRelayCommand SaveCommand => _saveCommand;

        private async Task SaveAsync()
        {
            Preferences.Set(KeyBirthDate, _birthDate);
            Preferences.Set(KeySex, _sex ?? string.Empty);

            if (_heightCm.HasValue) Preferences.Set(KeyHeightCm, _heightCm.Value); else Preferences.Remove(KeyHeightCm);
            if (_weightKg.HasValue) Preferences.Set(KeyWeightKg, _weightKg.Value); else Preferences.Remove(KeyWeightKg);

            Preferences.Remove(KeyStrideLengthCm);
            if (_strideLengthCm.HasValue) Preferences.Set(KeyStrideLengthCm, _strideLengthCm.Value);
            Preferences.Set(KeyManualStrideOverride, _manualStrideOverride);

            Preferences.Set(KeyDailyGoal, _dailyGoal);
            Preferences.Set(KeyReminderTime, _reminderTime.ToString(@"hh\:mm"));

            Preferences.Set(KeyUnits, _units);
            Preferences.Set(KeyWeekStart, _weekStart);
            Preferences.Set(KeyLanguage, _language);
            Preferences.Set(KeyDateFormat, _dateFormat);

            await Toast.Make("Settings saved", ToastDuration.Short).Show();
        }

        // ---- Helpers

        private void MaybeAutoComputeStride()
        {
            if (_manualStrideOverride) return;

            if (!_heightCm.HasValue || string.IsNullOrWhiteSpace(_sex))
            {
                _strideLengthCm = 75; // ~0.75 m default
                OnChanged(nameof(StrideLengthCm));
                return;
            }

            var estimate = EstimateStepLengthCm(_sex, _heightCm.Value);
            if (estimate.HasValue)
            {
                _strideLengthCm = (int)Math.Round(estimate.Value, MidpointRounding.AwayFromZero);
                OnChanged(nameof(StrideLengthCm));
            }
        }

        private void RefreshImperialHeightTextFromCm()
        {
            if (!_heightCm.HasValue)
            {
                _heightFeetText = "";
                _heightInchesText = "";
                return;
            }

            var totalInches = _heightCm.Value / 2.54;
            var feet = (int)Math.Floor(totalInches / 12.0);
            var inches = (int)Math.Round(totalInches - feet * 12, MidpointRounding.AwayFromZero);
            if (inches >= 12) { feet += 1; inches = 0; }

            _heightFeetText = feet.ToString(CultureInfo.InvariantCulture);
            _heightInchesText = inches.ToString(CultureInfo.InvariantCulture);
        }

        private void RecomputeHeightFromImperialTexts()
        {
            // Called whenever ft/in text changes
            int? feet = ParseNonNegativeInt(_heightFeetText);
            int? inches = ParseNonNegativeInt(_heightInchesText);

            if (!feet.HasValue && !inches.HasValue)
            {
                if (_heightCm != null)
                {
                    _heightCm = null;
                    _manualStrideOverride = false; // Option A: enable auto
                    OnChanged(nameof(HeightCm));
                    MaybeAutoComputeStride();
                }
                return;
            }

            int f = Math.Max(0, feet ?? 0);
            int i = Math.Max(0, inches ?? 0);
            if (i >= 12)
            {
                f += i / 12;
                i = i % 12;
                _heightFeetText = f.ToString(CultureInfo.InvariantCulture);
                _heightInchesText = i.ToString(CultureInfo.InvariantCulture);
                OnChanged(nameof(HeightFeetText));
                OnChanged(nameof(HeightInchesText));
            }

            var cm = Round1(f * 30.48 + i * 2.54);
            if (!NullableEquals(_heightCm, cm))
            {
                _heightCm = cm;
                _manualStrideOverride = false; // Option A
                OnChanged(nameof(HeightCm));
                MaybeAutoComputeStride();
            }
        }

        /// <summary>Estimate step length (cm) using height & sex (male≈0.413*height, female≈0.415*height).</summary>
        private static double? EstimateStepLengthCm(string sex, double heightCm)
        {
            if (heightCm <= 0) return null;
            double ratio = (sex?.Equals("Female", StringComparison.OrdinalIgnoreCase) ?? false) ? 0.415 : 0.413;
            return heightCm * ratio;
        }

        private void HandleUnitsChange(string oldUnits, string newUnits)
        {
            // Internal storage stays metric. Auto stride remains based on internal cm height.
            MaybeAutoComputeStride();
        }

        // ---- Safe getters / migration helpers ----

        private static string SafeGetString(string key, string @default)
        {
            try { return Preferences.Get(key, @default); }
            catch { return @default; }
        }

        private static bool SafeGetBool(string key, bool @default)
        {
            try { return Preferences.Get(key, @default); }
            catch
            {
                var s = Preferences.Get(key, (string)null);
                return bool.TryParse(s, out var b) ? b : @default;
            }
        }

        private static int SafeGetInt(string key, int @default)
        {
            try { return Preferences.Get(key, @default); }
            catch
            {
                var s = Preferences.Get(key, (string)null);
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
                try
                {
                    var d = Preferences.Get(key, 0.0);
                    if (!double.IsNaN(d) && !double.IsInfinity(d)) return (int)Math.Round(d);
                }
                catch { }
                return @default;
            }
        }

        private static int? SafeGetNullableIntWithMigration(string key)
        {
            if (!Preferences.ContainsKey(key)) return null;

            try { var i = Preferences.Get(key, 0); return i == 0 ? (int?)0 : i; }
            catch
            {
                var s = Preferences.Get(key, (string)null);
                if (!string.IsNullOrWhiteSpace(s) &&
                    int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;

                try
                {
                    var d = Preferences.Get(key, 0.0);
                    if (!double.IsNaN(d) && !double.IsInfinity(d))
                        return (int)Math.Round(d, MidpointRounding.AwayFromZero);
                }
                catch { }

                Preferences.Remove(key);
                return null;
            }
        }

        private static double? SafeGetNullableDouble(string key)
        {
            if (!Preferences.ContainsKey(key)) return null;

            try
            {
                var d = Preferences.Get(key, 0.0);
                if (double.IsNaN(d) || double.IsInfinity(d)) return null;
                return d;
            }
            catch
            {
                var s = Preferences.Get(key, (string)null);
                if (!string.IsNullOrWhiteSpace(s) &&
                    double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;

                Preferences.Remove(key);
                return null;
            }
        }

        private static DateTime SafeGetDateTime(string key, DateTime @default)
        {
            try { return Preferences.Get(key, @default); }
            catch
            {
                var s = Preferences.Get(key, (string)null);
                return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt) ? dt : @default;
            }
        }

        // ---- parsing & math helpers

        private static bool NullableEquals(double? a, double? b)
            => (!a.HasValue && !b.HasValue) || (a.HasValue && b.HasValue && Math.Abs(a.Value - b.Value) < 1e-9);

        private static double KgToLb(double kg) => kg * 2.2046226218;
        private static double LbToKg(double lb) => lb / 2.2046226218;

        private static double Round1(double v) => Math.Round(v, 1, MidpointRounding.AwayFromZero);

        // Accepts "6.2", "6,2", current culture, or invariant; trims spaces.
        private static double? ParseDoubleFlexible(object v)
        {
            if (v == null) return null;
            if (v is double d) return double.IsFinite(d) ? d : null;

            var s = Convert.ToString(v, CultureInfo.CurrentCulture)?.Trim();
            if (string.IsNullOrEmpty(s)) return null;

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out var cur) && double.IsFinite(cur))
                return cur;

            var s2 = s.Replace(',', '.');
            if (double.TryParse(s2, NumberStyles.Float, CultureInfo.InvariantCulture, out var inv) && double.IsFinite(inv))
                return inv;

            return null;
        }

        private static int? NormalizeInt(object v)
        {
            if (v == null) return null;
            if (v is int i) return i >= 0 ? i : null;

            var s = Convert.ToString(v, CultureInfo.CurrentCulture)?.Trim();
            if (string.IsNullOrEmpty(s)) return null;

            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.CurrentCulture, out var xi) && xi >= 0)
                return xi;

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out var xd) && xd >= 0)
                return (int)Math.Round(xd, MidpointRounding.AwayFromZero);

            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var yi) && yi >= 0)
                return yi;

            if (double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var yd) && yd >= 0)
                return (int)Math.Round(yd, MidpointRounding.AwayFromZero);

            return null;
        }

        private static int? ParseNonNegativeInt(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var v) && v >= 0)
                return v;
            if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) && v >= 0)
                return v;
            return null;
        }

        private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
