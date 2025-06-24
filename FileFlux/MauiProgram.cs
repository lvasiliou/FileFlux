using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using FileFlux.ViewModel;
using FileFlux.Services;
using Microsoft.Maui.LifecycleEvents;


#if WINDOWS10_0_17763_0_OR_GREATER
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
#endif

namespace FileFlux
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("FAS.otf", "FAS");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<HttpDownloadService>();
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<DownloadServiceFactory>();
            builder.Services.AddSingleton<OriginGuardFactory>();
            builder.Services.AddSingleton<DownloadManager>();
            builder.Services.AddTransient<HttpOriginGuard>();
            builder.Services.AddTransient<DownloadsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<NewDownloadViewModel>();
            

            builder.ConfigureLifecycleEvents(events =>
            {
#if WINDOWS10_0_17763_0_OR_GREATER

            events.AddWindows(wndLifeCycleBuilder =>
            {
                wndLifeCycleBuilder.OnWindowCreated(window =>
                {
                    window.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
                });
            });
#endif
            });

            return builder.Build();
        }

    }
}
