using MaterialDesignThemes.Wpf;

namespace ClassIsland.Core.Attributes;

/// <summary>
/// 提醒提供方信息
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class NotificationProviderInfo : Attribute
{
    /// <summary>
    /// 提醒提供方 GUID
    /// </summary>
    public Guid Guid { get; }

    /// <summary>
    /// 提醒提供方名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 提醒提供方图标
    /// </summary>
    public PackIconKind PackIcon { get; } = PackIconKind.BellRing;

    /// <summary>
    /// 提醒提供方位图图标uri
    /// </summary>
    public string BitmapIconUri { get; } = "";

    /// <summary>
    /// 是否使用位图图标
    /// </summary>
    public bool UseBitmapIcon { get; } = false;

    /// <summary>
    /// 提醒提供方描述
    /// </summary>
    public string Description { get; } = "";

    /// <summary>
    /// 提醒提供方设置界面类型
    /// </summary>
    public Type? SettingsType { get; internal set; }

    /// <summary>
    /// 提醒提供方类型
    /// </summary>
    public Type? ProviderType { get; internal set; }

    /// <summary>
    /// 是否注册了设置类型
    /// </summary>
    public bool HasSettings { get; internal set; }

    /// <summary>
    /// 已注册的提醒渠道
    /// </summary>
    public List<NotificationChannelInfo> RegisteredChannels { get; } = [];


    /// <inheritdoc />
    public NotificationProviderInfo(string guid, string name, PackIconKind icon, string description = "") : this(guid, name,
        description)
    {
        PackIcon = icon;
    }

    /// <inheritdoc />
    public NotificationProviderInfo(string guid, string name, string bitmapIconUri, string description = "") : this(guid, name,
        description)
    {
        BitmapIconUri = bitmapIconUri;
        UseBitmapIcon = true;
    }

    /// <inheritdoc />
    public NotificationProviderInfo(string guid, string name, string description = "")
    {
        Guid = Guid.Parse(guid);
        Name = name;
        Description = description;
    }
}