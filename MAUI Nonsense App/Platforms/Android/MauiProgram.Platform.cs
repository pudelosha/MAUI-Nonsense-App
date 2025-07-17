using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App
{
    public static partial class MauiProgram
    {
        static partial void ConfigurePlatformServices(MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<IStepCounterService, Platforms.Android.Services.StepCounter.AndroidStepCounterService>();
            builder.Services.AddTransient<ILightService, Platforms.Android.Services.Light.AndroidLightService>();
            builder.Services.AddSingleton<ICompassService, Platforms.Android.Services.Compass.AndroidCompassService>();
            builder.Services.AddSingleton<ILocationService, Platforms.Android.Services.Location.AndroidLocationService>();
            builder.Services.AddSingleton<IMovementAlarmService, Platforms.Android.Services.MovementAlarm.AndroidMovementAlarmService>();
            builder.Services.AddSingleton<IAlarmSoundService, Platforms.Android.Services.AlarmSound.AndroidAlarmSoundService>();
            builder.Services.AddSingleton<ILevelService, Platforms.Android.Services.Level.AndroidLevelService>();

        }
    }
}

