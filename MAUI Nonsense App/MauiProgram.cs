using Microsoft.Extensions.Logging;
using MAUI_Nonsense_App.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace MAUI_Nonsense_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if ANDROID
            builder.Services.AddSingleton<IStepCounterService, MAUI_Nonsense_App.Platforms.Android.Services.StepCounter.AndroidStepCounterService>();
            builder.Services.AddSingleton<ILightService, MAUI_Nonsense_App.Platforms.Android.Services.Light.AndroidLightService>();
#elif IOS
            builder.Services.AddSingleton<IStepCounterService, MAUI_Nonsense_App.Platforms.iOS.Services.StepCounter.iOSStepCounterService>();
            builder.Services.AddSingleton<ILightService, MAUI_Nonsense_App.Platforms.iOS.Services.Light.iOSLightService>();
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
