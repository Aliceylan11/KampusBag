using Microsoft.Extensions.Logging;

namespace KampusBag.MobileUI;

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

        // Plugin.Firebase.CloudMessaging standalone kullanılıyor.
        // google-services.json Android'de otomatik Firebase başlatır,
        // CrossFirebase.Initialize() çağrısına GEREK YOK.

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}