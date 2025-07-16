using MAUI_Nonsense_App.Services;
using Microsoft.Maui.ApplicationModel;

namespace MAUI_Nonsense_App.Pages.Survival;

public partial class CompassPage : ContentPage
{
    private readonly ICompassService _compassService;
    private readonly ILocationService _locationService;

    public CompassPage(ICompassService compassService, ILocationService locationService)
    {
        InitializeComponent();

        _compassService = compassService;
        _locationService = locationService;

        _compassService.HeadingChanged += OnHeadingChanged;
        _compassService.AccuracyChanged += OnAccuracyChanged;

        Appearing += OnAppearingAsync;
        Disappearing += (s, e) => _compassService.Stop();
    }

    private async void OnAppearingAsync(object sender, EventArgs e)
    {
        // Request Location permission cross–platform
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Permission Denied", "Location permission is required for compass to work properly.", "OK");
            return;
        }

        _compassService.Start(highAccuracy: true);

        var (lat, lon) = await _locationService.GetCurrentLocationAsync();
        var latDms = ToDms(lat, isLatitude: true);
        var lonDms = ToDms(lon, isLatitude: false);
        CoordinatesLabel.Text = $"{latDms}  {lonDms}";
    }

    private void OnHeadingChanged(object sender, double heading)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DialImage.Rotation = -heading;

            string cardinal = GetCardinalDirection(heading);
            HeadingLabel.Text = $"{heading:0}° {cardinal}";
        });
    }

    private void OnAccuracyChanged(object sender, CompassAccuracy accuracy)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CalibrationStatusLabel.Text = $"Calibration: {accuracy}";

            if (accuracy == CompassAccuracy.Unreliable || accuracy == CompassAccuracy.Low)
            {
                CalibrationStatusLabel.TextColor = Colors.Red;
                DisplayAlert("Calibration Needed", "Please shake and rotate your device to improve compass accuracy.", "OK");
            }
            else
            {
                CalibrationStatusLabel.TextColor = Colors.Green;
            }
        });
    }

    private void OnRecalibrateClicked(object sender, EventArgs e)
    {
        DisplayAlert("Recalibrate", "Please move and rotate your device in all directions until calibration improves.", "OK");
    }

    private string GetCardinalDirection(double heading)
    {
        if (heading >= 337.5 || heading < 22.5) return "N";
        if (heading >= 22.5 && heading < 67.5) return "NE";
        if (heading >= 67.5 && heading < 112.5) return "E";
        if (heading >= 112.5 && heading < 157.5) return "SE";
        if (heading >= 157.5 && heading < 202.5) return "S";
        if (heading >= 202.5 && heading < 247.5) return "SW";
        if (heading >= 247.5 && heading < 292.5) return "W";
        if (heading >= 292.5 && heading < 337.5) return "NW";
        return "";
    }

    private string ToDms(double decimalDegrees, bool isLatitude)
    {
        string direction;

        if (isLatitude)
            direction = decimalDegrees >= 0 ? "N" : "S";
        else
            direction = decimalDegrees >= 0 ? "E" : "W";

        decimalDegrees = Math.Abs(decimalDegrees);

        int degrees = (int)decimalDegrees;
        double fractional = (decimalDegrees - degrees) * 60;
        int minutes = (int)fractional;
        double seconds = (fractional - minutes) * 60;

        return $"{degrees}°{minutes}'{seconds:0}\" {direction}";
    }
}