using MAUI_Nonsense_App.Pages;
using MAUI_Nonsense_App.Pages.Activity;
using MAUI_Nonsense_App.Pages.Survival;
using MAUI_Nonsense_App.Services;
using Microsoft.Extensions.Logging;
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
                    fonts.AddFont("Font Awesome 6 Free-Solid-900", "FARegular");
                });

#if ANDROID
            builder.Services.AddSingleton<IStepCounterService, MAUI_Nonsense_App.Platforms.Android.Services.StepCounter.AndroidStepCounterService>();
            builder.Services.AddSingleton<ILightService, MAUI_Nonsense_App.Platforms.Android.Services.Light.AndroidLightService>();
#elif IOS
            builder.Services.AddSingleton<IStepCounterService, MAUI_Nonsense_App.Platforms.iOS.Services.StepCounter.iOSStepCounterService>();
            builder.Services.AddSingleton<ILightService, MAUI_Nonsense_App.Platforms.iOS.Services.Light.iOSLightService>();
#endif

            // Register all pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<StepCounterPage>();
            builder.Services.AddTransient<SurvivalPage>();
            builder.Services.AddTransient<ToolsPage>();
            builder.Services.AddTransient<OfficePage>();
            builder.Services.AddTransient<FinancePage>();
            builder.Services.AddTransient<QrScannerPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<LightPage>();
            builder.Services.AddTransient<CompassPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
