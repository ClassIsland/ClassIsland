using System.Runtime.Versioning;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Threading;
using ClassIsland.Android.Controls.UI;
using ClassIsland.Android.Services;
using ClassIsland.Android.Services.UI;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services.UI;
using ClassIsland.Core.Enums;
using ClassIsland.Shared;
using ClassIsland.Views;

namespace ClassIsland.Android;

[Activity(MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize,
    Theme = "@style/AppStartingTheme")]
[SupportedOSPlatform("android24.0")]
public class MainActivity : AvaloniaMainActivity
{
    private const string NotificationPermissionPreferencesName = "notification_permissions";
    private const string NotificationPermissionRequestedKey = "post_notifications_requested";
    private const int NotificationPermissionRequestCode = 13280;

    public static WeakReference<MainActivity>? Current { get; set; }

    public event EventHandler? Destroy;

    private AndroidViewHost? ViewHost { get; set; }

    public MainActivity()
    {
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        Current = new WeakReference<MainActivity>(this);
        
        IViewHostProvider.Instance = ActivityViewHostProvider.Instance;
        base.OnCreate(savedInstanceState);

        StartLessonsForegroundService();

        ViewHost = new AndroidViewHost(this);
        Content = ViewHost;
        ActivityViewHostProvider.Instance.ViewHosts.Add(ViewHost);
        
        var splash = new SplashView();
        splash.Show();

        RequestNotificationPermissionIfNeeded();

        if (AppBase.CurrentLifetime <= ApplicationLifetime.EarlyLoading)
        {
            Dispatcher.UIThread.Post(() =>
            {
                AppBase.Current.PhonyRootWindow = TopLevel.GetTopLevel(ViewHost)!;
                ((App)AppBase.Current).Init();
            });
        }
        else if (AppBase.CurrentLifetime == ApplicationLifetime.Running)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Console.WriteLine("[ELYSIADBG] Recreating MainView...");
                AppBase.Current.PhonyRootWindow = TopLevel.GetTopLevel(ViewHost)!;
                var mv = IAppHost.GetService<MainView>();
                mv.Show();
            });
        }
    }

    private void StartLessonsForegroundService()
    {
        var intent = new Intent(this, typeof(LessonsForegroundService));
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            StartForegroundService(intent);
        }
        else
        {
            StartService(intent);
        }
    }

    private void RequestNotificationPermissionIfNeeded()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
        {
            return;
        }

#pragma warning disable CA1416
        if (CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) == Permission.Granted)
        {
            return;
        }

        var preferences = GetSharedPreferences(
            NotificationPermissionPreferencesName,
            FileCreationMode.Private);
        if (preferences?.GetBoolean(NotificationPermissionRequestedKey, false) == true)
        {
            return;
        }

        preferences?.Edit()?
            .PutBoolean(NotificationPermissionRequestedKey, true)?
            .Apply();
        RequestPermissions(
            [global::Android.Manifest.Permission.PostNotifications],
            NotificationPermissionRequestCode);
#pragma warning restore CA1416
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
