using CoreLocation;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Platforms.iOS.Services.Location;

public class iOSLocationService : CLLocationManagerDelegate, ILocationService
{
    private readonly CLLocationManager _locationManager;

    private TaskCompletionSource<(double Latitude, double Longitude)>? _tcs;

    public iOSLocationService()
    {
        _locationManager = new CLLocationManager
        {
            DesiredAccuracy = CLLocation.AccuracyBest
        };
        _locationManager.Delegate = this;

        if (CLLocationManager.Status == CLAuthorizationStatus.NotDetermined)
        {
            _locationManager.RequestWhenInUseAuthorization();
        }
    }

    public Task<(double Latitude, double Longitude)> GetCurrentLocationAsync()
    {
        _tcs = new TaskCompletionSource<(double, double)>();
        _locationManager.StartUpdatingLocation();
        return _tcs.Task;
    }

    public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
    {
        var loc = locations.LastOrDefault();
        if (loc != null)
        {
            _locationManager.StopUpdatingLocation();
            _tcs?.TrySetResult((loc.Coordinate.Latitude, loc.Coordinate.Longitude));
        }
    }
}
