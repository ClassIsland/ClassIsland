using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.Actions;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Core.Models.Notification;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Hosting;
using ClassIsland.Helpers;

namespace ClassIsland.Services.NotificationProviders;

[NotificationProviderInfo("4B12F124-8585-43C7-AFC5-7BBB7CBE60D6", "行动提醒", PackIconKind.Airplane, "显示由行动发出的提醒。")]
public class ActionNotificationProvider : NotificationProviderBase
{
    public INotificationHostService NotificationHostService { get; }
    public IActionService ActionService { get; }

    public ActionNotificationProvider(INotificationHostService notificationHostService, IActionService actionService)
    {
        NotificationHostService = notificationHostService;
        ActionService = actionService;

        ActionService.RegisterActionHandler("classisland.showNotification", (o, s) => AppBase.Current.Dispatcher.Invoke(() => ActionHandler(o, s)));
    }

    private void ActionHandler(object? o, string guid)
    {
        if (o is not NotificationActionSettings settings)
        {
            return;
        }
        ShowNotification(new NotificationRequest()
        {
            MaskContent = NotificationContent.CreateTwoIconsMask(settings.Mask, hasRightIcon:false, factory: x =>
            {
                x.Duration = TimeSpanHelper.FromSecondsSafe(settings.MaskDurationSeconds);
                x.IsSpeechEnabled = settings.IsMaskSpeechEnabled;
            }),
            OverlayContent = string.IsNullOrWhiteSpace(settings.Content) || settings.ContentDurationSeconds <= 0 ? null : NotificationContent.CreateSimpleTextContent(settings.Content, factory: x =>
            {
                x.IsSpeechEnabled = settings.IsContentSpeechEnabled;
                x.Duration = TimeSpanHelper.FromSecondsSafe(settings.ContentDurationSeconds);
            }),
            RequestNotificationSettings =
            {
                IsSettingsEnabled = settings.IsAdvancedSettingsEnabled,
                IsNotificationEffectEnabled = settings.IsEffectEnabled,
                IsNotificationSoundEnabled = settings.IsSoundEffectEnabled,
                IsNotificationTopmostEnabled = settings.IsTopmostEnabled,
                NotificationSoundPath = settings.CustomSoundEffectPath,
                IsSpeechEnabled = true
            }
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}