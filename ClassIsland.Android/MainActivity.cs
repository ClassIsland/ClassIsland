using System.Runtime.Versioning;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Activity;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Threading;
using ClassIsland.Android.Controls.UI;
using ClassIsland.Android.Services;
using ClassIsland.Android.Services.Platform;
using ClassIsland.Android.Services.UI;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services.UI;
using ClassIsland.Core.Enums;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Shared;
using ClassIsland.Views;
#if DEBUG
using ClassIsland.Android.Controls;
#endif

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
    private const int LocationPermissionRequestCode = 13281;

    private TaskCompletionSource<bool>? _locationPermissionRequest;

    public static WeakReference<MainActivity>? Current { get; set; }

    internal static event EventHandler? Resumed;
    internal static event EventHandler? NotificationPermissionChanged;

    internal bool IsForeground { get; private set; }

    public event EventHandler? Destroy;

    private AndroidViewHost? ViewHost { get; set; }
    private OnBackPressedCallback? _backPressedCallback;
    private bool _isBackKeyHandled;
#if DEBUG
    private AndroidDebugOverlay? _debugOverlay;
    private bool _isReloadingViewHost;

    internal TopLevel? ViewHostRoot => ViewHost == null ? null : TopLevel.GetTopLevel(ViewHost);
#endif

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
#if DEBUG
        ViewHost.Loaded += DebugViewHostOnLoaded;
        _debugOverlay = new AndroidDebugOverlay(this, savedInstanceState);
#endif
        
        var splash = new SplashView();
        splash.Show();

        RequestNotificationPermissionIfNeeded();

        if (AppBase.CurrentLifetime <= ApplicationLifetime.EarlyLoading)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ((App)AppBase.Current).Init();
            });
        }
        else if (AppBase.CurrentLifetime == ApplicationLifetime.Running)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Console.WriteLine("[ELYSIADBG] Recreating MainView...");
                var mv = IAppHost.GetService<MainView>();
                mv.Show();
            });
        }

        HandleNotificationIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandleNotificationIntent(intent);
    }

    private static void HandleNotificationIntent(Intent? intent)
    {
        var notificationId = intent?.GetStringExtra(DesktopToastService.NotificationIdExtra);
        var actionId = intent?.GetStringExtra(DesktopToastService.ActionIdExtra);
        intent?.RemoveExtra(DesktopToastService.NotificationIdExtra);
        intent?.RemoveExtra(DesktopToastService.ActionIdExtra);

        if (Guid.TryParse(notificationId, out var notificationGuid) &&
            Guid.TryParse(actionId, out var actionGuid) &&
            PlatformServices.DesktopToastService is DesktopToastService service)
        {
            service.QueueActivation(notificationGuid, actionGuid);
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
    
    internal Task<bool> RequestLocationPermissionAsync()
    {
        if (_locationPermissionRequest != null)
        {
            return _locationPermissionRequest.Task;
        }

        if (!IsForeground || IsFinishing || IsDestroyed)
        {
            return Task.FromResult(false);
        }

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _locationPermissionRequest = completion;
        try
        {
            RequestPermissions(
                [global::Android.Manifest.Permission.AccessCoarseLocation,
                    global::Android.Manifest.Permission.AccessFineLocation],
                LocationPermissionRequestCode);
        }
        catch
        {
            _locationPermissionRequest = null;
            throw;
        }

        return completion.Task;
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode == NotificationPermissionRequestCode)
        {
            NotificationPermissionChanged?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (requestCode != LocationPermissionRequestCode)
        {
            return;
        }

        var completion = _locationPermissionRequest;
        _locationPermissionRequest = null;
        completion?.TrySetResult(grantResults.Length > 0 &&
            (CheckSelfPermission(global::Android.Manifest.Permission.AccessCoarseLocation) == Permission.Granted ||
             CheckSelfPermission(global::Android.Manifest.Permission.AccessFineLocation) == Permission.Granted));
    }

    protected override void OnStart()
    {
        base.OnStart();
        // Avalonia registers its callback in OnStart; register last so the host handles drawers first.
        _backPressedCallback = new HostBackPressedCallback(this);
        OnBackPressedDispatcher.AddCallback(this, _backPressedCallback);
    }

    protected override void OnStop()
    {
        _backPressedCallback?.Remove();
        _backPressedCallback = null;
        base.OnStop();
    }

    protected override void OnResume()
    {
        base.OnResume();
        IsForeground = true;
#if DEBUG
        _debugOverlay?.Resume();
#endif
        Resumed?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPause()
    {
        IsForeground = false;
#if DEBUG
        _debugOverlay?.Pause();
#endif
        base.OnPause();
    }

    protected override void OnDestroy()
    {
#if DEBUG
        _debugOverlay?.Dispose();
        _debugOverlay = null;
        if (ViewHost != null)
        {
            ViewHost.Loaded -= DebugViewHostOnLoaded;
        }
#endif
        _backPressedCallback?.Remove();
        _backPressedCallback = null;
        IsForeground = false;
        _locationPermissionRequest?.TrySetResult(false);
        _locationPermissionRequest = null;

        if (ViewHost != null)
        {
            ActivityViewHostProvider.Instance.ViewHosts.Remove(ViewHost);
        }

        Destroy?.Invoke(this, EventArgs.Empty);
        ViewHost = null;
        base.OnDestroy();
    }

#if DEBUG
    protected override void OnSaveInstanceState(Bundle outState)
    {
        _debugOverlay?.SaveState(outState);
        base.OnSaveInstanceState(outState);
    }

    private void DebugViewHostOnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        _debugOverlay?.ApplyOverlays();

    internal async Task ReloadViewHostAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (_isReloadingViewHost || IsFinishing || IsDestroyed ||
                AppBase.CurrentLifetime != ApplicationLifetime.Running || ViewHost == null)
            {
                throw new InvalidOperationException("应用尚未就绪，或视图宿主正在重载，请稍后再试。");
            }

            _isReloadingViewHost = true;
            try
            {
                var replacement = new AndroidViewHost(this);
                var previous = ViewHost;
                previous.Loaded -= DebugViewHostOnLoaded;
                try
                {
                    await previous.ReleaseHostAsync();
                }
                catch
                {
                    await replacement.ReleaseHostAsync();
                    throw;
                }
                if (IsFinishing || IsDestroyed)
                {
                    await replacement.ReleaseHostAsync();
                    return;
                }

                ActivityViewHostProvider.Instance.ViewHosts.Remove(previous);
                ViewHost = replacement;
                ViewHost.Loaded += DebugViewHostOnLoaded;
                ActivityViewHostProvider.Instance.ViewHosts.Add(ViewHost);
                Content = ViewHost;
                _isBackKeyHandled = false;
                _debugOverlay?.ApplyOverlays();
                var mainView = IAppHost.GetService<MainView>();
                ViewHost.ActivateView(mainView);
                await ViewHost.ShowView(mainView);
                mainView.ViewShown();
            }
            finally
            {
                _isReloadingViewHost = false;
            }
        });
    }
#endif

    public override bool DispatchKeyEvent(KeyEvent? e)
    {
        if (e?.KeyCode != Keycode.Back)
        {
            return base.DispatchKeyEvent(e);
        }

        if (e.Action == KeyEventActions.Down && e.RepeatCount == 0)
        {
            _isBackKeyHandled = ViewHost?.HandleBackRequested() == true;
        }
        if (!_isBackKeyHandled)
        {
            return base.DispatchKeyEvent(e);
        }

        // Avalonia maps Back to Escape on key down but forwards key up to Android's back dispatcher.
        // Consume the whole key press so closing a drawer cannot also navigate the underlying page.
        if (e.Action == KeyEventActions.Up)
        {
            _isBackKeyHandled = false;
        }
        return true;
    }

    public override void OnBackPressed()
    {
        if (_isBackKeyHandled || ViewHost?.HandleBackRequested() == true)
        {
            return;
        }

        base.OnBackPressed();
    }

    private void HandleHostBackPressed()
    {
        if (_isBackKeyHandled || ViewHost?.HandleBackRequested() == true)
        {
            return;
        }

        if (_backPressedCallback == null)
        {
            base.OnBackPressed();
            return;
        }

        _backPressedCallback.Enabled = false;
        try
        {
            OnBackPressedDispatcher.OnBackPressed();
        }
        finally
        {
            if (!IsFinishing && !IsDestroyed)
            {
                _backPressedCallback.Enabled = true;
            }
        }
    }

    private sealed class HostBackPressedCallback(MainActivity activity) : OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed() => activity.HandleHostBackPressed();
    }
}
