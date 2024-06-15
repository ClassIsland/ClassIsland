using System.Text.Json.Serialization;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Services.Registry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Components;

/// <summary>
/// 代表一个在主界面上显示的组件项目。
/// </summary>
public class ComponentSettings : ObservableRecipient
{
    /// <summary>
    /// 要显示的组件Id，ClassIsland用这个来索引组件，与<see cref="ComponentInfo"/>的Guid一致。
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// 组件名缓存。如果这个组件没有加载，会临时用这个名称来代替组件。
    /// </summary>
    public string NameCache { get; set; } = "";

    /// <summary>
    /// 组件的自定义设置
    /// </summary>
    public object? Settings { get; set; }

    /// <summary>
    /// 这个组件关联的组件注册信息。
    /// </summary>
    [JsonIgnore]
    public ComponentInfo AssociatedComponentInfo =>
        ComponentRegistryService.Registered.FirstOrDefault(x => string.Equals(x.Guid.ToString(), Id, StringComparison.CurrentCultureIgnoreCase)) ?? ComponentInfo.Empty;
}