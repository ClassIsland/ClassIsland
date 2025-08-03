using System.Text;
using Avalonia.Platform;
using Avalonia.Threading;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;
using DesktopNotifications;
using DesktopNotifications.FreeDesktop;
using Tmds.DBus;

namespace ClassIsland.Platforms.Linux.Services;

public class DesktopToastService : IDesktopToastService
{
    private Dictionary<string, Action> ActivationActions { get; } = new();

    private Dictionary<DesktopToastContent, List<string>> ActivationActionIds { get; } = new();
    
     private const string NotificationsService = "org.freedesktop.Notifications";

    private static readonly ObjectPath NotificationsPath = new ObjectPath("/org/freedesktop/Notifications");
    

    private readonly Dictionary<uint, DesktopToastContent> _activeNotifications = new();
    private IDisposable? _notificationActionSubscription;
    private IDisposable? _notificationCloseSubscription;
    private Connection? _connection;
    private IList<string> _capbilities = [];

    private IFreeDesktopNotificationsProxy? _proxy;

    public void Dispose()
    {
        _notificationActionSubscription?.Dispose();
        _notificationCloseSubscription?.Dispose();
    }

    public string? LaunchActionId { get; }

    public async Task Initialize()
    {
        _connection = Connection.Session;

        await _connection.ConnectAsync();

        _proxy = _connection.CreateProxy<IFreeDesktopNotificationsProxy>(
            NotificationsService,
            NotificationsPath
        );

        _notificationActionSubscription = await _proxy.WatchActionInvokedAsync(
            OnNotificationActionInvoked,
            OnNotificationActionInvokedError
        );

        _notificationCloseSubscription = await _proxy.WatchNotificationClosedAsync(
            OnNotificationClosed,
            OnNotificationClosedError
        );

        _capbilities = await _proxy.GetCapabilitiesAsync();
    }

    private async Task<string> GenerateBodyImage(Uri? imageUri)
    {
        // TODO: 在 kde 中向提醒里加入 <img/> 会导致图片大小异常，目前没有比较好的解决方案，
        // 先暂时禁用图片显示功能。
        return "";
        
        if (!_capbilities.Contains("body-images"))
        {
            return "";
        }

        var img = await PrepareToastImageResourceAsync(imageUri);
        if (img == null)
        {
            return "";
        }

        return $"""
                
                <img src="{img}" style="width: 100%; height: auto" width="100" alt=""/>
                
                """;
    }
    

    private async Task<string> GenerateNotificationBody(DesktopToastContent notification)
    {
        var sb = new StringBuilder();

        sb.Append(await GenerateBodyImage(notification.HeroImageUri));
        sb.Append(notification.Body);
        sb.Append(await GenerateBodyImage(notification.InlineImageUri));

        return sb.ToString();
    }

    private void OnNotificationClosed((uint id, uint reason) @event)
    {
        if (!_activeNotifications.Remove(@event.id, out var notification)) return;

        //TODO: Not sure why but it calls this event twice sometimes
        //In this case the notification has already been removed from the dict.
        if (notification == null)
        {
            return;
        }
        

        CleanupNotification(notification);CleanupNotification(notification);
    }

    private static void OnNotificationActionInvokedError(Exception obj)
    {
        throw obj;
    }

    private void OnNotificationActionInvoked((uint id, string actionKey) @event) =>
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!_activeNotifications.TryGetValue(@event.id, out var notification)) return;


            if (@event.actionKey == "default")
            {
                notification.Activated?.Invoke(this, EventArgs.Empty);
            }
            else if (ActivationActions.TryGetValue(@event.actionKey, out var action))
            {
                action();
            }

            CleanupNotification(notification);
        });
    
    private static void OnNotificationClosedError(Exception obj)
    {
        throw obj;
    }

    public async Task InitializeAsync()
    {
        await Initialize();
    }
    
    public async Task ShowToastAsync(DesktopToastContent content)
    {
        
        List<string> actions = [];
        List<string> actionsDbus = ["default", ""];

        var body = await GenerateNotificationBody(content);
        foreach (var (text, action) in content.Buttons)
        {
            var actionId = Guid.NewGuid().ToString();
            ActivationActions[actionId] = action;
            actions.Add(actionId);
            actionsDbus.Add(actionId);
            actionsDbus.Add(text);
        }

        var id = await _proxy!.NotifyAsync(
            "ClassIsland",
            0,
            (await PrepareToastImageResourceAsync(content.LogoImageUri))?.AbsolutePath ?? string.Empty,
            content.Title,
            body,
            actionsDbus.ToArray(),
            new Dictionary<string, object> { { "urgency", 1 } },
            5_000
        ).ConfigureAwait(false);

        ActivationActionIds[content] = actions;
        _activeNotifications[id] = content;
    }
    
    void CleanupNotification(DesktopToastContent toast)
    {
        if (!ActivationActionIds.TryGetValue(toast, out var actions))
        {
            return;
        }
        foreach (var i in actions)
        {
            ActivationActions.Remove(i);
        }

        ActivationActionIds.Remove(toast);
    }

    public async Task ShowToastAsync(string title, string body, Action? activated = null)
    {
        var desktopToastContent = new DesktopToastContent()
        {
            Title = title,
            Body = body
        };
        desktopToastContent.Activated += (_, _) => activated?.Invoke();
        await ShowToastAsync(desktopToastContent);
    }
    
    private async Task<Uri?> PrepareToastImageResourceAsync(Uri? sourceUri)
    {
        if (sourceUri == null)
        {
            return null;
        }
        
        try
        {
            switch (sourceUri.Scheme)
            {
                case "file":
                    return new Uri(sourceUri.AbsolutePath);
                case "avares":
                {
                    var stream = AssetLoader.Open(sourceUri);
                    var imagePath = Path.GetTempFileName();
                    await using var fileStream = File.Create(imagePath);
                    await stream.CopyToAsync(fileStream);
                    return new Uri(imagePath);
                }
                case "http" or "https":
                {
                    var imagePath = Path.GetTempFileName();
                    var client = new HttpClient();
                    var stream = await client.GetStreamAsync(sourceUri);
                    await using var fileStream = File.Create(imagePath);
                    await stream.CopyToAsync(fileStream);
                    return new Uri(imagePath);
                }
            }
        }
        catch (Exception)
        {
            return null;
        }
        return null;
    }

    public void ActivateNotificationAction(Guid id)
    {
        
    }
}