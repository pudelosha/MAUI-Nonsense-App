using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Platforms.Android.Services.Random
{
    public class AndroidCoinFlipService : ICoinFlipService
    {
        private readonly System.Random _random = new();

        public bool Toss()
        {
            return _random.Next(2) == 0; // true = Eagle, false = 1
        }
    }
}
