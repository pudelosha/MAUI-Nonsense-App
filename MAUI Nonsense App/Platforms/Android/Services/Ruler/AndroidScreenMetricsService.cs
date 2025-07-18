using MAUI_Nonsense_App.Services;
using AApp = Android.App.Application;

namespace MAUI_Nonsense_App.Platforms.Android.Services.Ruler
{
    public class AndroidScreenMetricsService : IScreenMetricsService
    {
        public double DpiX { get; }
        public double DpiY { get; }

        public double ScreenHeightPixels { get; }
        public double ScreenHeightInches { get; }

        public double Density { get; }

        public AndroidScreenMetricsService()
        {
            var metrics = AApp.Context.Resources?.DisplayMetrics;
            if (metrics is not null)
            {
                DpiX = metrics.Xdpi;
                DpiY = metrics.Ydpi;
                Density = metrics.Density;  // pixels per DIP

                ScreenHeightPixels = metrics.HeightPixels;
                ScreenHeightInches = ScreenHeightPixels / DpiY;

                Console.WriteLine($"[AndroidScreenMetricsService] DpiX: {DpiX:F2}, DpiY: {DpiY:F2}");
                Console.WriteLine($"[AndroidScreenMetricsService] Density: {Density:F2}");
                Console.WriteLine($"[AndroidScreenMetricsService] ScreenHeightPixels: {ScreenHeightPixels}, ScreenHeightInches: {ScreenHeightInches:F2}");
            }
            else
            {
                DpiX = DpiY = 160; // fallback mdpi
                Density = 1.0;
                ScreenHeightPixels = 1920; // fallback
                ScreenHeightInches = ScreenHeightPixels / DpiY;

                Console.WriteLine($"[AndroidScreenMetricsService] metrics is null, using fallback values.");
            }
        }
    }
}
