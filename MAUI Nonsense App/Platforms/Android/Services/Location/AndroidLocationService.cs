using Android.Content;
using Android.Locations;
using MAUI_Nonsense_App.Services;
using AApp = Android.App.Application;

namespace MAUI_Nonsense_App.Platforms.Android.Services.Location
{
    public class AndroidLocationService : ILocationService
    {
        public async Task<(double Latitude, double Longitude)> GetCurrentLocationAsync()
        {
            var context = AApp.Context ?? throw new InvalidOperationException("Application Context is null.");

            var locationManager = (LocationManager)context.GetSystemService(Context.LocationService)!;

            var provider = locationManager.GetBestProvider(new Criteria(), true);
            if (provider is null)
                return (0, 0);

            var location = locationManager.GetLastKnownLocation(provider);

            return location is null ? (0, 0) : (location.Latitude, location.Longitude);
        }
    }
}
