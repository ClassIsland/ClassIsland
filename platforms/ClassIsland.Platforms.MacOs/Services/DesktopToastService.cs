using Avalonia.Threading;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.MacOs.Services;

public class DesktopToastService : NSUserNotificationCenterDelegate, IDesktopToastService
{
    private Dictionary<string, Action> ActivationActions { get; } = new();

    private Dictionary<DesktopToastContent, List<string>> ActivationActionIds { get; } = new();
    
    private Dictionary<NSUserNotification, DesktopToastContent> ActiveNotifications { get; } = new();
    
    public DesktopToastService()
    {
        NSUserNotificationCenter.DefaultUserNotificationCenter.Delegate = this;
    }
    
    public Task ShowToastAsync(DesktopToastContent content)
    {
        var notificationId = Guid.NewGuid().ToString();
        var notification = new NSUserNotification()
        {
            Title = content.Title,
            InformativeText = content.Body,
            Identifier = notificationId,
        };
        var actionIds = new List<string>();
        var actions = new List<NSUserNotificationAction>();
        foreach (var (name, action) in content.Buttons)
        {
            var id = Guid.NewGuid().ToString();
            ActivationActions[id] = action;
            actionIds.Add(id);
            actions.Add(NSUserNotificationAction.GetAction(id, name));
        }

        notification.AdditionalActions = actions.ToArray();
        ActivationActionIds[content] = actionIds;
        ActiveNotifications[notification] = content;
        NSUserNotificationCenter.DefaultUserNotificationCenter.DeliverNotification(notification);
        return Task.CompletedTask;
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

    public void ActivateNotificationAction(Guid id)
    {
        
    }

    public override void DidActivateNotification(NSUserNotificationCenter center, NSUserNotification notification)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!ActiveNotifications.TryGetValue(notification, out var content))
            {
                return;
            }

            switch (notification.ActivationType)
            {
                case NSUserNotificationActivationType.ContentsClicked:
                    content.Activated?.Invoke(this, EventArgs.Empty);
                    break;
                case NSUserNotificationActivationType.AdditionalActionClicked:
                    var actionId = notification.AdditionalActivationAction?.Identifier;
                    if (actionId == null || !ActivationActions.TryGetValue(actionId, out var action))
                    {
                        break;
                    }

                    action();
                    break;
                case NSUserNotificationActivationType.None:
                case NSUserNotificationActivationType.ActionButtonClicked:
                case NSUserNotificationActivationType.Replied:
                default:
                    break;
            }

            CleanupNotification(content);
        });
    }
    
    private void CleanupNotification(DesktopToastContent toast)
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

    public override bool ShouldPresentNotification(NSUserNotificationCenter center, NSUserNotification notification)
    {
        return true;
    }
}