using System;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Management;
using ClassIsland.Shared.Protobuf.Command;
using ClassIsland.Shared.Protobuf.Enum;

using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.NotificationProviders;

[NotificationProviderInfo("0117fb4f-5374-434a-97fb-4f5374634a07", "集控提醒", PackIconKind.Work, "来自集控服务器的提醒。")]
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
        var payload = SendNotification.Parser.ParseFrom(e.Payload);
        if (payload == null)
            return;
        Logger.LogInformation("接受集控消息：{} {}", payload.MessageMask, payload.MessageContent);
        ShowNotification(new NotificationRequest()
        {
            MaskContent = NotificationContent.CreateTwoIconsMask(payload.MessageMask, rightIcon:PackIconKind.Announcement),
            OverlayContent = NotificationContent.CreateRollingTextContent(payload.MessageContent, TimeSpan.FromSeconds(payload.DurationSeconds) * payload.RepeatCounts, payload.RepeatCounts),
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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}