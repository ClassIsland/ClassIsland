using System;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Core.Interfaces;
using ClassIsland.Core.Models.Management;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Protobuf.Command;
using ClassIsland.Core.Protobuf.Enum;
using ClassIsland.Services.Management;

using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.NotificationProviders;

public class ManagementNotificationProvider : INotificationProvider, IHostedService
{
    public string Name { get; set; } = "集控提醒";
    public string Description { get; set; } = "来自集控服务器的提醒。";
    public Guid ProviderGuid { get; set; } = new Guid("0117fb4f-5374-434a-97fb-4f5374634a07");
    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; } = new PackIcon()
    {
        Kind = PackIconKind.Work,
        Width = 24,
        Height = 24
    };
    
    private NotificationHostService NotificationHostService { get; }
    
    private ManagementService ManagementService { get; }
    
    private ILogger<ManagementNotificationProvider> Logger { get; }

    public ManagementNotificationProvider(NotificationHostService notificationHostService, 
        ManagementService managementService,
        ILogger<ManagementNotificationProvider> logger)
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
        NotificationHostService.ShowNotification(new NotificationRequest()
        {
            MaskContent = new ManagementNotificationProviderControl(true, payload),
            MaskSpeechContent = payload.MessageMask,
            OverlayContent = new ManagementNotificationProviderControl(false, payload),
            OverlayDuration = TimeSpan.FromSeconds(payload.DurationSeconds) * payload.RepeatCounts,
            OverlaySpeechContent = payload.MessageContent,
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