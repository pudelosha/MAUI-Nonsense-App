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
            builder.Services.AddSingleton<IMovementAlarmService, Platforms.Android.Services.MovementAlarm.AndroidMovementAlarmService>();
            builder.Services.AddSingleton<IAlarmSoundService, Platforms.Android.Services.AlarmSound.AndroidAlarmSoundService>();
            builder.Services.AddSingleton<ILevelService, Platforms.Android.Services.Level.AndroidLevelService>();
            builder.Services.AddSingleton<IScreenMetricsService, Platforms.Android.Services.Ruler.AndroidScreenMetricsService>();
            builder.Services.AddSingleton<IRandomNumberService, Platforms.Android.Services.Random.AndroidRandomNumberService>();
            builder.Services.AddSingleton<ICoinFlipService, Platforms.Android.Services.Random.AndroidCoinFlipService>();
            builder.Services.AddSingleton<IDiceRollService, Platforms.Android.Services.Random.AndroidDiceRollService>();

        }
    }
}

