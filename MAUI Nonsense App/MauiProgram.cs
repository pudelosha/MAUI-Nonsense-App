using MAUI_Nonsense_App.Pages;
using MAUI_Nonsense_App.Pages.Activity;
using MAUI_Nonsense_App.Pages.Survival;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace MAUI_Nonsense_App
{
    public static partial class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Font Awesome 6 Free-Solid-900", "FARegular");
                });

            // Call platform-specific service registration
            ConfigurePlatformServices(builder);

            // Register all pages (transient)
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

        // Declared here so shared code compiles — implemented in platform .cs files
        static partial void ConfigurePlatformServices(MauiAppBuilder builder);
    }
}
