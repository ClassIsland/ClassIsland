using System.Text;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Plugin;
using System.Text.Json.Serialization;
namespace ClassIsland.Core.Attributes;

/// 贡献者信息。用于为应用内的功能标识其贡献者与插件来源。<br/>
/// 添加此信息需要遵守规范。<br/>
/// https://docs.classisland.tech/dev/contributor-attribution.html
[AttributeUsage(AttributeTargets.All)]
public class ContributorInfo(string details) : Attribute
{
    /// 插件 ID。设置此项以自动获取插件名称和插件支持信息。
    public string? PluginId { get; set; }

    /// 该功能的贡献者详情。
    public string? Details { get; set; } = details;

    /// 插件名称。
    [JsonIgnore] public string? PluginName => PluginInfo?.Manifest.Name;

    /// 插件支持信息。
    [JsonIgnore] public string? PluginMessage => CustomizedPluginMessage ?? (_defaultPluginMessage ??= BuildDefaultMessage(PluginInfo));

    /// 已解析的插件信息。
    [JsonIgnore] public PluginInfo? PluginInfo
    {
        get
        {
            if (_pluginInfo is null && PluginId is not null)
            {
                _pluginInfo = IPluginService.LoadedPlugins
                    .FirstOrDefault(p => p.Manifest.Id == PluginId);
            }
            return _pluginInfo;
        }
        internal set => _pluginInfo = value;
    }

    private PluginInfo? _pluginInfo;
    private string? _defaultPluginMessage;
    internal string? CustomizedPluginMessage;

    private static string? BuildDefaultMessage(PluginInfo? plugin)
    {
        if (plugin is null) return null;
        var m = plugin.Manifest;
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(m.Name))
            sb.AppendLine($"插件  **{m.Name}**");
        if (!string.IsNullOrEmpty(m.Author))
            sb.AppendLine($"作者  @{m.Author}");
        sb.Append($"项目主页  [插件详情页](classisland://app/settings/classisland.plugins?pluginId={m.Id}&ci_keepHistory=true)");
        if (!string.IsNullOrEmpty(m.Url))
            sb.Append($" [在浏览器中打开↗]({m.Url})");
        sb.AppendLine();
        sb.AppendLine();
        sb.Append("如需获取帮助或反馈问题，请访问项目主页。");
        return sb.ToString();
    }

    internal ContributorInfo() : this(null!) { }

    /// <see cref="ContributorInfo"/>
    public static implicit operator ContributorInfo(string details) => new(details);

    public override string ToString() => $"[{PluginId}|{Details}]";
}
