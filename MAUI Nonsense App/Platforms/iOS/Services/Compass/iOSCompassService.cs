using CoreLocation;
using Foundation;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Platforms.iOS.Services.Compass;

public class iOSCompassService : NSObject, ICompassService, ICLLocationManagerDelegate
{
    private readonly CLLocationManager _locationManager;

    public event EventHandler<double>? HeadingChanged;
    public event EventHandler<CompassAccuracy>? AccuracyChanged;

    public iOSCompassService()
    {
        _locationManager = new CLLocationManager
        {
            DesiredAccuracy = CLLocation.AccuracyBestForNavigation
        };
        _locationManager.Delegate = this;

        if (CLLocationManager.HeadingAvailable)
        {
            _locationManager.RequestWhenInUseAuthorization();
        }
    }

    public void Start(bool highAccuracy = true)
    {
        if (CLLocationManager.HeadingAvailable)
        {
            _locationManager.StartUpdatingHeading();
        }
    }

    public void Stop()
    {
        _locationManager.StopUpdatingHeading();
    }

    [Export("locationManager:didUpdateHeading:")]
    public void UpdatedHeading(CLLocationManager manager, CLHeading newHeading)
    {
        HeadingChanged?.Invoke(this, newHeading.TrueHeading);

        // Map accuracy level
        CompassAccuracy accuracy =
            newHeading.HeadingAccuracy <= 5 ? CompassAccuracy.High :
            newHeading.HeadingAccuracy <= 15 ? CompassAccuracy.Medium :
            newHeading.HeadingAccuracy <= 30 ? CompassAccuracy.Low :
            CompassAccuracy.Unreliable;

        AccuracyChanged?.Invoke(this, accuracy);
    }

    [Export("locationManagerShouldDisplayHeadingCalibration:")]
    public bool ShouldDisplayHeadingCalibration(CLLocationManager manager)
    {
        // Always allow calibration UI when needed
        return true;
    }
}
