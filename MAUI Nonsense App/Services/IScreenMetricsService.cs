using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_Nonsense_App.Services
{
    public interface IScreenMetricsService
    {
        double DpiX { get; }
        double DpiY { get; }

        double ScreenHeightPixels { get; }
        double ScreenHeightInches { get; }

        double Density { get; }
    }
}
