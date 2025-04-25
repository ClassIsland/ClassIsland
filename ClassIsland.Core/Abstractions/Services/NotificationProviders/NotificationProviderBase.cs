using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.Core.Abstractions.Services.NotificationProviders;

/// <summary>
/// 提醒提供方基类。
/// </summary>
public abstract class NotificationProviderBase : INotificationProvider, IHostedService
{
    private object? _settingsElement;

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public Guid ProviderGuid { get; set; }

    /// <inheritdoc />
    public object? SettingsElement
    {
        get
        {
            if (_settingsElement == null && Info.SettingsType != null)
            {
                SetupSettingsControl(Info.HasSettings);
            }
            return _settingsElement;
        }
        set => _settingsElement = value;
    }

    /// <inheritdoc />
    public object? IconElement { get; set; }

    [NotNull] internal object SettingsInternal { get; set; } = null;

    // ReSharper disable once InconsistentNaming
    internal INotificationHostService __NotificationHostService { get; } = IAppHost.GetService<INotificationHostService>();

    private NotificationProviderInfo Info { get; }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }

    /// <summary>
    /// 初始化一个 <see cref="NotificationProviderBase"/> 类的新实例。
    /// </summary>
    protected NotificationProviderBase() : this(true)
    {
    }

    /// <summary>
    /// 初始化一个 <see cref="NotificationProviderBase"/> 类的新实例。
    /// </summary>
    protected NotificationProviderBase(bool autoRegister)
    {
        var info = NotificationProviderRegistryService.RegisteredProviders.FirstOrDefault(x => x.ProviderType == GetType());

        Info = info ?? throw new InvalidOperationException($"没有找到与 {GetType()} 对应的提醒提供方。");

        Name = info.Name;
        Description = info.Description;
        ProviderGuid = info.Guid;
        if (info.UseBitmapIcon)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(info.BitmapIconUri, UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();
            IconElement = new Image()
            {
                Source = bitmapImage,
                Width = 24,
                Height = 24,
                Stretch = Stretch.UniformToFill
            };
        }
        else
        {
            IconElement = new PackIcon()
            {
                Kind = info.PackIcon,
                Width = 24,
                Height = 24
            };
        }

        if (!autoRegister)
        {
            return;
        }
        __NotificationHostService.RegisterNotificationProvider(this);
    }

    private void SetupSettingsControl(bool hasSettings)
    {
        var settings = hasSettings ? SettingsInternal : null;
        _settingsElement = NotificationProviderControlBase.GetInstance(Info, ref settings);
        __NotificationHostService.WriteNotificationProviderSettings(ProviderGuid, settings);
    }

    /// <summary>
    /// 显示一个提醒。
    /// </summary>
    /// <param name="request">提醒请求</param>
    protected void ShowNotification(NotificationRequest request)
    {
        __NotificationHostService.ShowNotification(request, ProviderGuid);
    }

    /// <summary>
    /// 显示一个提醒，并等待提醒显示完成。
    /// </summary>
    /// <param name="request">提醒请求</param>
    protected async Task ShowNotificationAsync(NotificationRequest request)
    {
        await __NotificationHostService.ShowNotificationAsync(request, ProviderGuid);
    }

    /// <summary>
    /// 显示链式提醒。链式显示的提醒会按照传入的顺序显示，并且当其中一个提醒被取消时，所有后续的提醒都会被取消。
    /// </summary>
    /// <param name="requests">提醒请求</param>
    protected void ShowChainedNotifications(params NotificationRequest[] requests)
    {
        __NotificationHostService.ShowChainedNotifications(requests, ProviderGuid);
    }

    /// <summary>
    /// 显示链式提醒，并等待最后一个提醒显示完成。链式显示的提醒会按照传入的顺序显示，并且当其中一个提醒被取消时，所有后续的提醒都会被取消。
    /// </summary>
    /// <param name="requests">提醒请求</param>
    protected async Task ShowChainedNotificationsAsync(NotificationRequest[] requests)
    {
        await __NotificationHostService.ShowChainedNotificationsAsync(requests, ProviderGuid);
    }

}

/// <inheritdoc />
public abstract class NotificationProviderBase<TSettings> : NotificationProviderBase where TSettings : class
{
    /// <summary>
    /// 当前提醒提供方的设置
    /// </summary>
    public TSettings Settings => (SettingsInternal as TSettings)!;

    /// <inheritdoc />
    protected NotificationProviderBase() : this(true)
    {
    }

    /// <inheritdoc />
    protected NotificationProviderBase(bool autoRegister)
    {
        if (!autoRegister)
        {
            return;
        }
        SettingsInternal = __NotificationHostService.GetNotificationProviderSettings<TSettings>(ProviderGuid);
    }
}