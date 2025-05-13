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
public abstract class NotificationProviderBase : INotificationProvider, INotificationSender, IHostedService
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
    internal INotificationHostService __NotificationHostService { get; } =
        IAppHost.GetService<INotificationHostService>();

    private NotificationProviderInfo Info { get; }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }

    internal Dictionary<Guid, NotificationChannel> Channels { get; } = new();

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

    /// <inheritdoc />
    public void ShowNotification(NotificationRequest request)
    {
        __NotificationHostService.ShowNotification(request, ProviderGuid, Guid.Empty);
    }

    /// <inheritdoc />
    public async Task ShowNotificationAsync(NotificationRequest request)
    {
        await __NotificationHostService.ShowNotificationAsync(request, ProviderGuid, Guid.Empty);
    }

    /// <inheritdoc />
    public void ShowChainedNotifications(params NotificationRequest[] requests)
    {
        __NotificationHostService.ShowChainedNotifications(requests, ProviderGuid, Guid.Empty);
    }

    /// <inheritdoc />
    public async Task ShowChainedNotificationsAsync(NotificationRequest[] requests)
    {
        await __NotificationHostService.ShowChainedNotificationsAsync(requests, ProviderGuid, Guid.Empty);
    }

    /// <summary>
    /// 获取指定的提醒渠道
    /// </summary>
    /// <param name="id">提醒渠道 GUID</param>
    /// <returns>对应的提醒渠道 <see cref="NotificationChannel"/></returns>
    protected NotificationChannel Channel(string id)
    {
        return Channel(Guid.Parse(id));
    }

    /// <summary>
    /// 获取指定的提醒渠道
    /// </summary>
    /// <param name="id">提醒渠道 GUID</param>
    /// <returns>对应的提醒渠道 <see cref="NotificationChannel"/></returns>
    protected NotificationChannel Channel(Guid id)
    {
        if (Channels.TryGetValue(id, out var value))
        {
            return value;
        }

        throw new InvalidOperationException($"无效的提醒提供方 ID {id}");
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