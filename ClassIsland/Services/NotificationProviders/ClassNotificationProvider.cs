using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Interfaces;
using ClassIsland.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services.NotificationProviders;

public class ClassNotificationProvider : INotificationProvider, IHostedService
{
    public string Name { get; set; } = "上课提醒";
    public string Description { get; set; } = "在准备上课、上课和下课时发出醒目提醒，并预告下一节课程。";
    public Guid ProviderGuid { get; set; } = new Guid("08F0D9C3-C770-4093-A3D0-02F3D90C24BC");
    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; } = new PackIcon()
    {
        Kind = PackIconKind.Notifications,
        Width = 32,
        Height = 32
    };

    private ClassNotificationSettings Settings
    {
        get;
        set;
    } = new();

    private NotificationHostService NotificationHostService { get; }

    public ClassNotificationProvider(NotificationHostService notificationHostService)
    {
        NotificationHostService = notificationHostService;
        NotificationHostService.RegisterNotificationProvider(this);
        NotificationHostService.OnClass += (sender, args) =>
        {
            notificationHostService.RequestQueue.Enqueue(new NotificationRequest()
            {
                MaskContent = new ClassNotificationProviderControl("ClassOnNotification")
            });
        };
        Settings = NotificationHostService.GetNotificationProviderSettings
                       <ClassNotificationSettings>(this.ProviderGuid)
                   ?? new ClassNotificationSettings();
        SettingsElement = new ClassNotificationProviderSettingsControl(Settings);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return new Task(() => {});
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return new Task(() => { });
    }
}