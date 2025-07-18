using MAUI_Nonsense_App.Services;
using UIKit;

namespace MAUI_Nonsense_App.Platforms.iOS.Services.Ruler
{
    public class iOSScreenMetricsService : IScreenMetricsService
    {
        public double DpiX { get; }
        public double DpiY { get; }

        public double ScreenHeightPixels { get; }
        public double ScreenHeightInches { get; }

        public double Density { get; }

        public iOSScreenMetricsService()
        {
            var screen = UIScreen.MainScreen;

            Density = screen.Scale; // pixels per DIP
            var sizeInPoints = screen.Bounds.Size;
            var sizeInPixels = screen.NativeBounds.Size;

            // iPhone base DPI: ~163ppi at 1x. Scale accordingly.
            double estimatedBaseDpi = 163.0;
            double dpi = estimatedBaseDpi * Density;

            DpiX = dpi;
            DpiY = dpi;

            ScreenHeightPixels = sizeInPixels.Height;
            ScreenHeightInches = ScreenHeightPixels / DpiY;

            Console.WriteLine($"[iOSScreenMetricsService] DpiX: {DpiX:F2}, DpiY: {DpiY:F2}");
            Console.WriteLine($"[iOSScreenMetricsService] Density: {Density:F2}");
            Console.WriteLine($"[iOSScreenMetricsService] ScreenHeightPixels: {ScreenHeightPixels}, ScreenHeightInches: {ScreenHeightInches:F2}");
        }
    }
}
