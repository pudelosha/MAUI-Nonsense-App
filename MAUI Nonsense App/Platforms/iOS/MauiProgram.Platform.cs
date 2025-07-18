using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App
{
    public static partial class MauiProgram
    {
        static partial void ConfigurePlatformServices(MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<IStepCounterService, Platforms.iOS.Services.StepCounter.iOSStepCounterService>();
            builder.Services.AddTransient<ILightService, Platforms.iOS.Services.Light.iOSLightService>();
            builder.Services.AddSingleton<ICompassService, Platforms.iOS.Services.Compass.iOSCompassService>();
            builder.Services.AddSingleton<ILocationService, Platforms.iOS.Services.Location.iOSLocationService>();
            builder.Services.AddSingleton<IMovementAlarmService, Platforms.iOS.Services.MovementAlarm.iOSMovementAlarmService>();
            builder.Services.AddSingleton<IAlarmSoundService, Platforms.iOS.Services.AlarmSound.iOSAlarmSoundService>();
            builder.Services.AddSingleton<ILevelService, Platforms.iOS.Services.Level.iOSLevelService>();
            builder.Services.AddSingleton<IScreenMetricsService, Platforms.iOS.Services.Ruler.iOSScreenMetricsService>();


        }
    }
}
