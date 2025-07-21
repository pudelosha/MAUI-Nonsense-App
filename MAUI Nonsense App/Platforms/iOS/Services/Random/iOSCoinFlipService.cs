using MAUI_Nonsense_App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_Nonsense_App.Platforms.iOS.Services.Random
{
    public class iOSCoinFlipService : ICoinFlipService
    {
        private readonly System.Random _random = new();

        public bool Toss()
        {
            return _random.Next(2) == 0; // true = Eagle, false = 1
        }
    }
}
