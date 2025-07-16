using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App
{
    public static partial class MauiProgram
    {
        static partial void ConfigurePlatformServices(MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<IStepCounterService, Platforms.Android.Services.StepCounter.AndroidStepCounterService>();
            builder.Services.AddSingleton<ILightService, Platforms.Android.Services.Light.AndroidLightService>();
            builder.Services.AddSingleton<ICompassService, Platforms.Android.Services.Compass.AndroidCompassService>();
            builder.Services.AddSingleton<ILocationService, Platforms.Android.Services.Location.AndroidLocationService>();
        }
    }
}

