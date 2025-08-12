using CommunityToolkit.Maui;
using MAUI_Nonsense_App.Pages;
using MAUI_Nonsense_App.Pages.Activity;
using MAUI_Nonsense_App.Pages.Office;
using MAUI_Nonsense_App.Pages.Random;
using MAUI_Nonsense_App.Pages.Survival;
using MAUI_Nonsense_App.Pages.Tools;
using MAUI_Nonsense_App.ViewModels;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using CommunityToolkit.Maui;
using MAUI_Nonsense_App.Pages.Games;

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
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Font Awesome 6 Free-Solid-900.otf", "FASolid");
                    fonts.AddFont("Font Awesome 6 Brands-Regular-400.otf", "FABrands");
                });

            // Call platform-specific service registration
            ConfigurePlatformServices(builder);

            // Register all pages (transient)
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<StepCounterPage>();
            builder.Services.AddTransient<SurvivalPage>();
            builder.Services.AddTransient<ToolsPage>();
            builder.Services.AddTransient<OfficePage>();
            builder.Services.AddTransient<GamesPage>();
            builder.Services.AddTransient<QrScannerPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<LightPage>();
            builder.Services.AddTransient<CompassPage>();
            builder.Services.AddTransient<MovementDetectorPage>();
            builder.Services.AddTransient<LevelPage>();
            builder.Services.AddTransient<RulerPage>();
            builder.Services.AddTransient<ProtractorPage>();
            builder.Services.AddTransient<UnitConverterPage>();
            builder.Services.AddTransient<MirrorPage>();
            builder.Services.AddTransient<VibrometerPage>();
            builder.Services.AddTransient<MetalDetectorPage>();
            builder.Services.AddTransient<RandomPage>();
            builder.Services.AddTransient<RandomNumberPage>();
            builder.Services.AddTransient<CoinFlipPage>();
            builder.Services.AddTransient<DicePage>();
            builder.Services.AddTransient<RandomSpinnerPage>();
            builder.Services.AddTransient<RouletteWheelPage>();
            builder.Services.AddTransient<SavePdfPage>();
            builder.Services.AddTransient<ImageArrangePage>();
            builder.Services.AddTransient<ImageSelectionPage>();
            builder.Services.AddTransient<ImageEditorPage>();
            builder.Services.AddTransient<ImageToPdfPage>();
            builder.Services.AddTransient<SnakePage>();
            builder.Services.AddTransient<TetrisPage>();
            builder.Services.AddTransient<ArkanoidPage>();
            builder.Services.AddTransient<_2048Page>();

            builder.Services.AddTransient<MovementDetectorViewModel>();
            builder.Services.AddTransient<LightViewModel>();
            builder.Services.AddTransient<QrScannerViewModel>();
            builder.Services.AddTransient<StepCounterViewModel>();
            builder.Services.AddTransient<LevelViewModel>();
            builder.Services.AddTransient<RulerViewModel>();
            builder.Services.AddTransient<ProtractorViewModel>();
            builder.Services.AddTransient<RandomNumberViewModel>();
            builder.Services.AddTransient<CoinFlipViewModel>(); 
            builder.Services.AddTransient<DiceViewModel>();
            builder.Services.AddTransient<RouletteViewModel>();
            builder.Services.AddTransient<RandomSpinnerViewModel>();
            builder.Services.AddTransient<SavePdfViewModel>();
            builder.Services.AddTransient<ImageArrangeViewModel>();
            builder.Services.AddTransient<ImageSelectionViewModel>();
            builder.Services.AddTransient<SnakeViewModel>();
            builder.Services.AddTransient<TetrisViewModel>();
            builder.Services.AddTransient<ArkanoidViewModel>();
            builder.Services.AddTransient<Game2048ViewModel>();


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        // Declared here so shared code compiles — implemented in platform .cs files
        static partial void ConfigurePlatformServices(MauiAppBuilder builder);
    }
}
