using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_Nonsense_App.Services
{
    public interface ILightService
    {
        Task TurnOnAsync();
        Task TurnOffAsync();
        bool IsOn { get; }
        Task<bool> IsSupportedAsync();
        Task SetBrightnessAsync(double strength); // 0.0 - 1.0
        Task StartLighthouseAsync();
        Task StopLighthouseAsync();
        Task StartPoliceAsync();
        Task StopPoliceAsync();
        Task StartStrobeAsync(int intervalMs);
        Task StopStrobeAsync();
        Task StartSOSAsync();
        Task StopSOSAsync();
        Task StartMorseAsync(string morse);
    }
}
