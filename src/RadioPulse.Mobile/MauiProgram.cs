using Microsoft.Extensions.Logging;
using RadioPulse.Mobile.Pages;
using RadioPulse.Mobile.Services;

namespace RadioPulse.Mobile;

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

        builder.Services.AddSingleton<SessionState>();
        builder.Services.AddSingleton<RadioApiService>();
        builder.Services.AddSingleton<EngagementSignalRService>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<NowPlayingPage>();
        builder.Services.AddTransient<EngagementPage>();
        builder.Services.AddTransient<RecommendationsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        var app = builder.Build();
        ServiceHelper.Services = app.Services;
        return app;
    }
}
