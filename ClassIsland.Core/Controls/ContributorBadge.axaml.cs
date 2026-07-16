using Avalonia;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using static Avalonia.AvaloniaProperty;
namespace ClassIsland.Core.Controls;

/// <summary>
/// 贡献者徽章。用于为应用内的功能标识其贡献者与插件来源。<br/>
/// 添加此控件需要遵守规范。<br/>
/// https://docs.classisland.tech/dev/contributor-attribution.html
/// </summary>
public partial class ContributorBadge : UserControl
{
    /// <inheritdoc cref="ContributorBadge"/>
    public ContributorBadge() => InitializeComponent();

    /// 该功能的贡献者信息。
    public ContributorInfo ContributorInfo
    {
        get => _contributorInfo;
        set
        {
            _contributorInfo = value ?? new();
            RaisePropertyChanged(PluginNameProperty, null, PluginName);
            RaisePropertyChanged(MarkdownProperty, null, Markdown);
        }
    }
    ContributorInfo _contributorInfo = new();
    public static readonly DirectProperty<ContributorBadge, ContributorInfo?> ContributorInfoProperty =
        RegisterDirect<ContributorBadge, ContributorInfo?>(nameof(ContributorInfo), o => o.ContributorInfo, (o, v) => o.ContributorInfo = v);

    /// 提供该功能的插件 id。
    public string? PluginId
    {
        get => ContributorInfo.PluginId;
        set
        {
            if (value == "ClassIsland")
                ContributorInfo.IsBuiltIn = true;
            else
                ContributorInfo.PluginId = value;
        }
    }

    public static readonly DirectProperty<ContributorBadge, string?> PluginIdProperty =
        RegisterDirect<ContributorBadge, string?>(nameof(PluginId), o => o.PluginId, (o, v) => o.PluginId = v);

    public string? PluginName => ContributorInfo.PluginName;
    public static readonly DirectProperty<ContributorBadge, string?> PluginNameProperty =
        RegisterDirect<ContributorBadge, string?>(nameof(PluginName), o => o.PluginName);
    
    /// 该功能的贡献者详情。
    public string? Details
    {
        get => ContributorInfo.Details;
        set
        {
            ContributorInfo.Details = value;
            RaisePropertyChanged(MarkdownProperty, null, Markdown);
        }
    }

    public string Markdown
    {
        get
        {
            var msg = ContributorInfo.PluginMessage;
            IsVisible = true;
            if (msg == null)
            {
                if (Details == null)
                {
                    IsVisible = !string.IsNullOrEmpty(PluginId);
                    return $"插件名称  **{PluginId}**";
                }
                else
                    return Details;
            }
            else
            {
                if (Details == null)
                {
                    if (ContributorInfo.IsBuiltIn)
                    {
                        IsVisible = false;
                    }
                    return msg;
                }
                else
                    return $@"{Details}\\{msg}";
            }
        }
    }
    public static readonly DirectProperty<ContributorBadge, string> MarkdownProperty =
        RegisterDirect<ContributorBadge, string>(nameof(Markdown), o => o.Markdown);
}
