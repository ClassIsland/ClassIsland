using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Enums;
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
        Width = 24,
        Height = 24
    };

    private ClassNotificationSettings Settings
    {
        get;
        set;
    } = new();

    private bool IsClassPreparingNotified { get; set; } = false;

    private NotificationHostService NotificationHostService { get; }

    public ClassNotificationProvider(NotificationHostService notificationHostService)
    {
        NotificationHostService = notificationHostService;
        NotificationHostService.RegisterNotificationProvider(this);
        NotificationHostService.OnClass += OnClass;
        NotificationHostService.OnBreakingTime += OnBreakingTime;
        NotificationHostService.UpdateTimerTick += UpdateTimerTick;
        Settings = NotificationHostService.GetNotificationProviderSettings
                       <ClassNotificationSettings>(ProviderGuid)
                   ?? new ClassNotificationSettings();
        SettingsElement = new ClassNotificationProviderSettingsControl(Settings);
        NotificationHostService.WriteNotificationProviderSettings(ProviderGuid, Settings);
    }

    private void UpdateTimerTick(object? sender, EventArgs e)
    {
        var tClassDelta = NotificationHostService.OnClassDeltaTime;
        if (Settings.IsClassOnPreparingNotificationEnabled &&
            ((tClassDelta > TimeSpan.Zero &&
              tClassDelta <= TimeSpan.FromSeconds(Settings.InDoorClassPreparingDeltaTime) &&
              !NotificationHostService.NextClassSubject.IsOutDoor) // indoor
             ||
             (tClassDelta > TimeSpan.Zero &&
              tClassDelta <= TimeSpan.FromSeconds(Settings.OutDoorClassPreparingDeltaTime) &&
              NotificationHostService.NextClassSubject.IsOutDoor) // outdoor
            ) &&
            !IsClassPreparingNotified)
        {
            IsClassPreparingNotified = true;
            NotificationHostService.RequestQueue.Enqueue(new NotificationRequest()
            {
                MaskContent = new ClassNotificationProviderControl("ClassPrepareNotifyMask"),
                MaskDuration = TimeSpan.FromSeconds(5),
                OverlayContent = new ClassNotificationProviderControl("ClassPrepareNotifyOverlay"),
                TargetOverlayEndTime = DateTimeToCurrentDateTimeConverter.Convert(NotificationHostService.NextClassTimeLayoutItem.StartSecond)
            });

            NotificationHostService.RequestQueue.Enqueue(new NotificationRequest()
            {
                MaskContent = new ClassNotificationProviderControl("ClassOnNotification")
            });
        }
    }

    private void OnBreakingTime(object? sender, EventArgs e)
    {
        IsClassPreparingNotified = false;
        if (!Settings.IsClassOffNotificationEnabled)
        {
            return;
        }
        NotificationHostService.RequestQueue.Enqueue(new NotificationRequest()
        {
            MaskContent = new ClassNotificationProviderControl("ClassOffNotification"),
            MaskDuration = TimeSpan.FromSeconds(2),
            OverlayContent = new ClassNotificationProviderControl("ClassOffOverlay"),
            OverlayDuration = TimeSpan.FromSeconds(10)
        });
    }

    private void OnClass(object? sender, EventArgs e)
    {
        if (!Settings.IsClassOnNotificationEnabled)
        {
            return;
        }

        if (IsClassPreparingNotified)
        {
            IsClassPreparingNotified = false;
            return;
        }
        NotificationHostService.RequestQueue.Enqueue(new NotificationRequest()
        {
            MaskContent = new ClassNotificationProviderControl("ClassOnNotification")
        });
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