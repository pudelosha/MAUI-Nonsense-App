using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_Nonsense_App.Services
{
    public interface IMovementAlarmService
    {
        void Configure(int armingDelaySeconds, int sensitivity);
        Task StartAsync();
        Task StopAsync();
        event EventHandler MovementDetected;
    }
}
