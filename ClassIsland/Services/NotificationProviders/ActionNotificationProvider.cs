using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.Actions;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services.NotificationProviders;

public class ActionNotificationProvider : INotificationProvider, IHostedService
{
    public INotificationHostService NotificationHostService { get; }
    public IActionService ActionService { get; }
    public string Name { get; set; } = "行动提醒";
    public string Description { get; set; } = "显示由行动发出的提醒。";
    public Guid ProviderGuid { get; set; } = new Guid("4B12F124-8585-43C7-AFC5-7BBB7CBE60D6");
    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; } = new PackIcon()
    {
        Kind = PackIconKind.Airplane,
        Width = 24,
        Height = 24
    };

    public ActionNotificationProvider(INotificationHostService notificationHostService, IActionService actionService)
    {
        NotificationHostService = notificationHostService;
        ActionService = actionService;

        NotificationHostService.RegisterNotificationProvider(this);
        ActionService.RegisterActionHandler("classisland.showNotification", (o, s) => AppBase.Current.Dispatcher.Invoke(() => ActionHandler(o, s)));
    }

    private void ActionHandler(object? o, string guid)
    {
        if (o is not NotificationActionSettings settings)
        {
            return;
        }
        NotificationHostService.ShowNotification(new NotificationRequest()
        {
            MaskContent = new StackPanel()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new PackIcon(){ Kind = PackIconKind.InfoCircleOutline, Width = 22, Height = 22, VerticalAlignment = VerticalAlignment.Center},
                    new TextBlock(new Run(settings.Mask)){FontSize = 18, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0), FontWeight = FontWeights.Bold}
                }
            },
            OverlayContent = string.IsNullOrWhiteSpace(settings.Content) || settings.ContentDurationSeconds <= 0 ? null : new TextBlock(new Run(settings.Content)) { FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center},
            IsSpeechEnabled = settings.IsContentSpeechEnabled || settings.IsMaskSpeechEnabled,
            MaskSpeechContent = settings.IsMaskSpeechEnabled ? settings.Mask : "",
            OverlaySpeechContent = settings.IsContentSpeechEnabled ? settings.Content : "",
            MaskDuration = TimeSpan.FromSeconds(settings.MaskDurationSeconds),
            OverlayDuration = TimeSpan.FromSeconds(settings.ContentDurationSeconds),
            RequestNotificationSettings =
            {
                IsSettingsEnabled = settings.IsAdvancedSettingsEnabled,
                IsNotificationEffectEnabled = settings.IsEffectEnabled,
                IsNotificationSoundEnabled = settings.IsSoundEffectEnabled,
                IsNotificationTopmostEnabled = settings.IsTopmostEnabled,
                NotificationSoundPath = settings.CustomSoundEffectPath
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