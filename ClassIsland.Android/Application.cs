using System.Runtime.Versioning;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Media;
using ClassIsland.Extensions;

namespace ClassIsland.Android;

[Application]
[SupportedOSPlatform("android")]
public class Application : AvaloniaAndroidApplication<App>
{
    public static Application Instance { get; private set; } = null!;
    
    protected Application(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
        Instance = this;
    }
    
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        var buildApp = Program.AppEntry(["--mobile"]);

        return AppBuilder.Configure<App>(() =>
            {
                var app = buildApp();
                app.OperatingSystem = "windows";
                return app;
            })
            .UseAndroid()
            .With(new FontManagerOptions
            {
                DefaultFamilyName = MainWindow.DefaultFontFamilyKey,
                FontFallbacks =
                [
                    new FontFallback
                    {
                        FontFamily = MainWindow.DefaultFontFamily
                    }
                ]
            })
            .LogToHostSink();
    }
}