using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BirdStrikeAnalyzer.Model;

namespace BirdStrikeAnalyzer;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });

        builder.Services.AddSingleton<IDatabase, Database>();
        builder.Services.AddSingleton<BusinessLogic>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MapViewPage>();
        builder.Services.AddTransient<BirdStrikesPage>();

        // Analyzer starts with in-memory example data. Replace IAnalyzerDataSource
        // registration with an adapter that calls BusinessLogic/IDatabase when ready.
        builder.Services.AddSingleton<IAnalyzerDataSource, SampleAnalyzerDataSource>();
        builder.Services.AddTransient<AnalyzerPage>();

        Preferences.Set(
            GeoapifyMapView.ApiKeyPreference,
            "20e9fa2b068745e6af3c620f1a49f211"
        );
        

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
