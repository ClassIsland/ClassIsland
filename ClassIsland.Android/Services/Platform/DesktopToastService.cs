using System.Runtime.Versioning;
using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using AndroidX.Core.App;
using Avalonia.Platform;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Enums;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Android.Services.Platform;

[SupportedOSPlatform("android24.0")]
public class DesktopToastService : IDesktopToastService, IDisposable
{
    private const string NotificationChannelId = "desktop_toasts";
    internal const string NotificationIdExtra = "desktop_toast_notification_id";
    internal const string ActionIdExtra = "desktop_toast_action_id";

    private static readonly HttpClient ImageHttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly Context _context = Application.Instance;
    private readonly NotificationManager _notificationManager;
    private readonly Dictionary<Guid, List<Guid>> _notifications = new();
    private readonly Dictionary<Guid, (Guid NotificationId, Action Callback)> _actions = new();
    private readonly Queue<Guid> _pendingActivations = new();
    private bool _isWaitingForAppStart;
    private bool _disposed;

    public DesktopToastService()
    {
        _notificationManager = (NotificationManager)_context.GetSystemService(Context.NotificationService)!;
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            using var channel = new NotificationChannel(NotificationChannelId, "应用通知",
                NotificationImportance.Default)
            {
                Description = "显示 ClassIsland 的应用消息和操作提示"
            };
            _notificationManager.CreateNotificationChannel(channel);
        }
        MainActivity.Resumed += ActivityOnResumed;
    }

    public async Task ShowToastAsync(DesktopToastContent content)
    {
        if (!await Dispatcher.UIThread.InvokeAsync(CanPostNotification))
        {
            return;
        }

        using var logo = await LoadImageAsync(content.LogoImageUri);
        using var picture = await LoadImageAsync(content.HeroImageUri) ??
                            await LoadImageAsync(content.InlineImageUri);
        await Dispatcher.UIThread.InvokeAsync(() => ShowNotification(content, logo, picture));
    }

    public Task ShowToastAsync(string title, string body, Action? activated = null) =>
        ShowToastAsync(new DesktopToastContent
        {
            Title = title,
            Body = body,
            Activated = (_, _) => activated?.Invoke()
        });

    private bool CanPostNotification()
    {
        using var manager = NotificationManagerCompat.From(_context);
        if (_disposed || manager?.AreNotificationsEnabled() != true)
        {
            return false;
        }
        if (OperatingSystem.IsAndroidVersionAtLeast(33) &&
            _context.CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) != Permission.Granted)
        {
            return false;
        }
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            using var channel = _notificationManager.GetNotificationChannel(NotificationChannelId);
            if (channel?.Importance == NotificationImportance.None)
            {
                return false;
            }
        }
        return true;
    }

    private void ShowNotification(DesktopToastContent content, Bitmap? logo, Bitmap? picture)
    {
        if (!CanPostNotification())
        {
            return;
        }

        var notificationId = Guid.NewGuid();
        _notifications[notificationId] = [];
        try
        {
            using var builder = new NotificationCompat.Builder(_context, NotificationChannelId);
            builder.SetSmallIcon(ResourceConstant.Drawable.ic_logo_monochrome_notification);
            builder.SetContentTitle(content.Title);
            builder.SetContentText(content.Body);
            builder.SetPriority(NotificationCompat.PriorityDefault);
            builder.SetAutoCancel(true);
            if (logo != null)
            {
                builder.SetLargeIcon(logo);
            }
            if (picture != null)
            {
                using var style = new NotificationCompat.BigPictureStyle();
                style.BigPicture(picture);
                style.SetSummaryText(content.Body);
                builder.SetStyle(style);
            }
            else
            {
                using var style = new NotificationCompat.BigTextStyle();
                style.BigText(content.Body);
                builder.SetStyle(style);
            }

            using var contentIntent = CreateActivationIntent(notificationId,
                () => content.Activated?.Invoke(this, EventArgs.Empty));
            builder.SetContentIntent(contentIntent);
            foreach (var (text, action) in content.Buttons.Take(3))
            {
                using var actionIntent = CreateActivationIntent(notificationId, action);
                builder.AddAction(0, text, actionIntent);
            }
            using var deleteIntent = new Intent(_context, typeof(DesktopToastDismissReceiver));
            deleteIntent.SetAction(notificationId.ToString());
            deleteIntent.PutExtra(NotificationIdExtra, notificationId.ToString());
            using var dismissIntent = PendingIntent.GetBroadcast(_context, 0, deleteIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            builder.SetDeleteIntent(dismissIntent);

            using var notification = builder.Build()!;
            _notificationManager.Notify(notificationId.ToString(), 0, notification);
        }
        catch (Java.Lang.SecurityException) when (!CanPostNotification())
        {
            // Notification permission can be revoked while the notification is being built.
            DismissNotification(notificationId);
        }
        catch
        {
            DismissNotification(notificationId);
            throw;
        }
    }

    private PendingIntent CreateActivationIntent(Guid notificationId, Action callback)
    {
        var actionId = Guid.NewGuid();
        _actions[actionId] = (notificationId, callback);
        _notifications[notificationId].Add(actionId);

        using var intent = new Intent(_context, typeof(MainActivity));
        // Extras do not participate in PendingIntent identity.
        intent.SetAction(actionId.ToString());
        intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        intent.PutExtra(NotificationIdExtra, notificationId.ToString());
        intent.PutExtra(ActionIdExtra, actionId.ToString());
        return PendingIntent.GetActivity(_context, 0, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }

    private static Task<Bitmap?> LoadImageAsync(Uri? uri) => Task.Run(async () =>
    {
        if (uri == null)
        {
            return null;
        }
        try
        {
            await using var source = uri.Scheme switch
            {
                "file" => File.OpenRead(uri.LocalPath),
                "avares" => AssetLoader.Open(uri),
                "http" or "https" => await ImageHttpClient.GetStreamAsync(uri),
                _ => null
            };
            if (source == null)
            {
                return null;
            }

            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer);
            using var options = new BitmapFactory.Options { InJustDecodeBounds = true };
            buffer.Position = 0;
            BitmapFactory.DecodeStream(buffer, null, options)?.Dispose();
            options.InJustDecodeBounds = false;
            options.InSampleSize = 1;
            // Bound bitmap size before decoding to keep notification parcels and memory usage small.
            while (Math.Max(options.OutWidth, options.OutHeight) / options.InSampleSize > 1024)
            {
                options.InSampleSize *= 2;
            }
            buffer.Position = 0;
            return BitmapFactory.DecodeStream(buffer, null, options);
        }
        catch (Exception)
        {
            return null;
        }
    });

    internal void QueueActivation(Guid notificationId, Guid actionId)
    {
        RunOnUiThread(() =>
        {
            if (_disposed)
            {
                return;
            }
            if (!_actions.TryGetValue(actionId, out var action) || action.NotificationId != notificationId)
            {
                DismissNotification(notificationId);
                return;
            }

            _pendingActivations.Enqueue(actionId);
            if (AppBase.CurrentLifetime < ApplicationLifetime.Running && !_isWaitingForAppStart)
            {
                _isWaitingForAppStart = true;
                AppBase.Current.AppStarted += AppOnStarted;
            }
            Dispatcher.UIThread.Post(ProcessPendingActivations, DispatcherPriority.Background);
        });
    }

    private void ActivityOnResumed(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(ProcessPendingActivations, DispatcherPriority.Background);

    private void AppOnStarted(object? sender, EventArgs e)
    {
        AppBase.Current.AppStarted -= AppOnStarted;
        _isWaitingForAppStart = false;
        Dispatcher.UIThread.Post(ProcessPendingActivations, DispatcherPriority.Background);
    }

    private void ProcessPendingActivations()
    {
        if (_disposed || AppBase.CurrentLifetime != ApplicationLifetime.Running ||
            MainActivity.Current?.TryGetTarget(out var activity) != true || activity?.IsForeground != true)
        {
            return;
        }
        while (_pendingActivations.TryDequeue(out var actionId))
        {
            ActivateNotificationAction(actionId);
        }
    }

    public void ActivateNotificationAction(Guid id)
    {
        RunOnUiThread(() =>
        {
            if (_disposed || !_actions.TryGetValue(id, out var action))
            {
                return;
            }
            DismissNotification(action.NotificationId);
            action.Callback();
        });
    }

    internal void DismissNotification(Guid notificationId)
    {
        RunOnUiThread(() =>
        {
            if (_notifications.Remove(notificationId, out var actions))
            {
                foreach (var actionId in actions)
                {
                    _actions.Remove(actionId);
                }
            }
            _notificationManager.Cancel(notificationId.ToString(), 0);
        });
    }

    private static void RunOnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }

    public void Dispose()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.InvokeAsync(Dispose).GetAwaiter().GetResult();
            return;
        }
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        MainActivity.Resumed -= ActivityOnResumed;
        if (_isWaitingForAppStart)
        {
            AppBase.Current.AppStarted -= AppOnStarted;
            _isWaitingForAppStart = false;
        }
        foreach (var notificationId in _notifications.Keys.ToArray())
        {
            DismissNotification(notificationId);
        }
        _pendingActivations.Clear();
        GC.SuppressFinalize(this);
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
[SupportedOSPlatform("android24.0")]
public sealed class DesktopToastDismissReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (Guid.TryParse(intent?.GetStringExtra(DesktopToastService.NotificationIdExtra), out var id) &&
            PlatformServices.DesktopToastService is DesktopToastService service)
        {
            service.DismissNotification(id);
        }
    }
}
