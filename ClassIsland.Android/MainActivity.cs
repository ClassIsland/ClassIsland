using System.Runtime.Versioning;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Threading;
using ClassIsland.Android.Controls.UI;
using ClassIsland.Android.Services.UI;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services.UI;
using ClassIsland.Core.Enums;
using ClassIsland.Views;

namespace ClassIsland.Android;

[Activity(Label = "ClassIsland",
    MainLauncher = true, 
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize,
    Theme = "@style/AppTheme")]
[SupportedOSPlatform("android24.0")]
public class MainActivity : AvaloniaMainActivity
{
    public event EventHandler? Destroy;

    private AndroidViewHost? ViewHost { get; set; }

    public MainActivity()
    {
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        IViewHostProvider.Instance = ActivityViewHostProvider.Instance;
        base.OnCreate(savedInstanceState);

        ViewHost = new AndroidViewHost(this);
        Content = ViewHost;
        ActivityViewHostProvider.Instance.ViewHosts.Add(ViewHost);
        
        var splash = new SplashView();
        splash.Show();

        if (AppBase.CurrentLifetime <= ApplicationLifetime.EarlyLoading)
        {
            Dispatcher.UIThread.Post(() =>
            {
                AppBase.Current.PhonyRootWindow = TopLevel.GetTopLevel(ViewHost)!;
                ((App)AppBase.Current).Init();
            });
        }
    }
    
    protected override void OnDestroy()
    {
        if (ViewHost != null)
        {
            ActivityViewHostProvider.Instance.ViewHosts.Remove(ViewHost);
        }

        Destroy?.Invoke(this, EventArgs.Empty);
        ViewHost = null;
        base.OnDestroy();
    }
}
