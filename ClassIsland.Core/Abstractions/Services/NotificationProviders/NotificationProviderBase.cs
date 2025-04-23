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
    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public Guid ProviderGuid { get; set; }

    /// <inheritdoc />
    public object? SettingsElement { get; set; }

    /// <inheritdoc />
    public object? IconElement { get; set; }

    [NotNull] internal object SettingsInternal { get; set; } = null;

    internal INotificationHostService NotificationHostService { get; } = IAppHost.GetService<INotificationHostService>();

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
    protected NotificationProviderBase()
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

        NotificationHostService.RegisterNotificationProvider(this);

        if (!Info.HasSettings)
        {
            SetupSettingsControl(false);
        }
    }

    internal void SetupSettingsControl(bool hasSettings)
    {
        var settings = hasSettings ? SettingsInternal : null;
        SettingsElement = NotificationProviderControlBase.GetInstance(Info, ref settings);
        NotificationHostService.WriteNotificationProviderSettings(ProviderGuid, settings);
    }

    /// <summary>
    /// 显示一个提醒。
    /// </summary>
    /// <param name="request">提醒请求</param>
    protected void ShowNotification(NotificationRequest request)
    {
        NotificationHostService.ShowNotification(request, ProviderGuid);
    }

    /// <summary>
    /// 显示一个提醒，并等待提醒显示完成。
    /// </summary>
    /// <param name="request">提醒请求</param>
    protected async Task ShowNotificationAsync(NotificationRequest request)
    {
        await NotificationHostService.ShowNotificationAsync(request, ProviderGuid);
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
    protected NotificationProviderBase()
    {
        SettingsInternal = NotificationHostService.GetNotificationProviderSettings<TSettings>(ProviderGuid);
        SetupSettingsControl(true);
    }
}