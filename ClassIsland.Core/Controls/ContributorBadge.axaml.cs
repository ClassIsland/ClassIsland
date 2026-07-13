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

    // protected override Size MeasureOverride(Size availableSize)
    // {
    //     // 先尝试展开状态
    //     RootButton.IsKeepingExpanded = true;
    //     var expandedSize = base.MeasureOverride(availableSize);
    //
    //     // 如果展开状态宽度超出可用空间，且可用空间有限，回退到紧凑模式（仅图标）
    //     if (expandedSize.Width > availableSize.Width && double.IsFinite(availableSize.Width))
    //     {
    //         RootButton.IsKeepingExpanded = false;
    //         return base.MeasureOverride(availableSize);
    //     }
    //
    //     return expandedSize;
    // }

    /// 该功能的贡献者信息。
    public ContributorInfo? ContributorInfo
    {
        get => _contributorInfo;
        set
        {
            _contributorInfo = value;
            RaisePropertyChanged(PluginNameProperty, null, PluginName);
            RaisePropertyChanged(MarkdownProperty, null, Markdown);
        }
    }
    ContributorInfo? _contributorInfo;
    public static readonly DirectProperty<ContributorBadge, ContributorInfo?> ContributorInfoProperty =
        RegisterDirect<ContributorBadge, ContributorInfo?>(nameof(ContributorInfo), o => o.ContributorInfo, (o, v) => o.ContributorInfo = v);

    /// 提供该功能的插件名称。
    public string? PluginName
    {
        get => ContributorInfo?.PluginName ?? _pluginName;
        set
        {
            _pluginName = value;
            RaisePropertyChanged(PluginNameProperty, null, PluginName);
        }
    }
    string? _pluginName;
    public static readonly DirectProperty<ContributorBadge, string?> PluginNameProperty =
        RegisterDirect<ContributorBadge, string?>(nameof(PluginName), o => o.PluginName, (o, v) => o.PluginName = v);
    
    /// 该功能的贡献者描述。
    public string? Details
    {
        get => ContributorInfo?.Details ?? _details;
        set
        {
            _details = value;
            RaisePropertyChanged(MarkdownProperty, null, Markdown);
        }
    }
    string? _details;

    public string Markdown
    {
        get
        {
            var msg = ContributorInfo?.PluginMessage;
            Opacity = 1.0;
            if (msg == null)
            {
                if (Details == null)
                {
                    Opacity = !string.IsNullOrEmpty(PluginName) ? 1.0 : 0.0;
                    return $"插件名称  **{PluginName}**";
                }
                else
                    return Details;
            }
            else
            {
                if (Details == null)
                    return msg;
                else
                    return $@"{Details}\\{msg}";
            }
        }
    }
    public static readonly DirectProperty<ContributorBadge, string> MarkdownProperty =
        RegisterDirect<ContributorBadge, string>(nameof(Markdown), o => o.Markdown);
}
