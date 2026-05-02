using System;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared.Models.Management;
using ClassIsland.Shared.Protobuf.Command;
using ClassIsland.Shared.Protobuf.Enum;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.NotificationProviders;

[NotificationProviderInfo("0117fb4f-5374-434a-97fb-4f5374634a07", "集控提醒", "\uE821", "来自集控服务器的提醒。")]
public class ManagementNotificationProvider : NotificationProviderBase
{
    private INotificationHostService NotificationHostService { get; }

    private IManagementService ManagementService { get; }

    private ILogger<ManagementNotificationProvider> Logger { get; }

    public ManagementNotificationProvider(INotificationHostService notificationHostService,
        IManagementService managementService,
        ILogger<ManagementNotificationProvider> logger) : base(false)
    {
        NotificationHostService = notificationHostService;
        ManagementService = managementService;
        Logger = logger;

        if (!ManagementService.IsManagementEnabled)
            return;
        NotificationHostService.RegisterNotificationProvider(this);
        if (ManagementService.Connection != null)
        {
            ManagementService.Connection.CommandReceived += ConnectionOnCommandReceived;
        }
    }

    private void ConnectionOnCommandReceived(object? sender, ClientCommandEventArgs e)
    {
        if (e.Type != CommandTypes.SendNotification)
            return;
        SendNotification payload;
        try
        {
            payload = SendNotification.Parser.ParseFrom(e.Payload);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "解析集控通知指令失败。");
            return;
        }

        if (payload == null)
            return;

        var repeatCounts = Math.Max(1, payload.RepeatCounts);
        var durationSeconds = payload.DurationSeconds <= 0 ? 5 : payload.DurationSeconds;
        var maskText = string.IsNullOrWhiteSpace(payload.MessageMask) ? "集控通知" : payload.MessageMask;
        var messageContent = string.IsNullOrWhiteSpace(payload.MessageContent) ? "" : payload.MessageContent;

        Logger.LogInformation("接受集控消息：{} {}", payload.MessageMask, payload.MessageContent);
        ShowNotification(new NotificationRequest()
        {
            MaskContent = NotificationContent.CreateTwoIconsMask(maskText, rightIcon: "\uE7E7"),
            OverlayContent = NotificationContent.CreateRollingTextContent(messageContent, TimeSpan.FromSeconds(durationSeconds) * repeatCounts, repeatCounts),
            IsPriorityOverride = payload.IsEmergency,
            PriorityOverride = -1,
            RequestNotificationSettings =
            {
                IsSettingsEnabled = true,
                IsSpeechEnabled = payload.IsSpeechEnabled,
                IsNotificationEffectEnabled = payload.IsEffectEnabled,
                IsNotificationSoundEnabled = payload.IsSoundEnabled,
                IsNotificationTopmostEnabled = payload.IsTopmost
            }
        });
    }
}