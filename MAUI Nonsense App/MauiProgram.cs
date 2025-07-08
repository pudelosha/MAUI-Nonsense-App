using Microsoft.Extensions.Logging;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if ANDROID
            builder.Services.AddSingleton<IStepCounterService, MAUI_Nonsense_App.Platforms.Android.Services.AndroidStepCounterService>();
            builder.Services.AddSingleton<IQrScannerService, MAUI_Nonsense_App.Platforms.Android.Services.AndroidQrScannerService>();
#elif IOS
            builder.Services.AddSingleton<IStepCounterService, MAUI_Nonsense_App.Platforms.iOS.Services.iOSStepCounterService>();
            builder.Services.AddSingleton<IQrScannerService, MAUI_Nonsense_App.Platforms.iOS.Services.iOSQrScannerService>();
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
