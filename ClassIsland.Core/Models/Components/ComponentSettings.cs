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
    private bool _hideOnRule = false;
    private object? _settings;
    private string _nameCache = "";
    private string _id = "";
    private Ruleset.Ruleset _hidingRules = new();

    /// <summary>
    /// 要显示的组件Id，ClassIsland用这个来索引组件，与<see cref="ComponentInfo"/>的Guid一致。
    /// </summary>
    public string Id
    {
        get => _id;
        set
        {
            if (value == _id) return;
            _id = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AssociatedComponentInfo));
        }
    }

    /// <summary>
    /// 组件名缓存。如果这个组件没有加载，会临时用这个名称来代替组件。
    /// </summary>
    public string NameCache
    {
        get => _nameCache;
        set
        {
            if (value == _nameCache) return;
            _nameCache = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 组件的自定义设置
    /// </summary>
    public object? Settings
    {
        get => _settings;
        set
        {
            if (Equals(value, _settings)) return;
            _settings = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否在条件满足时自动隐藏
    /// </summary>
    public bool HideOnRule
    {
        get => _hideOnRule;
        set
        {
            if (value == _hideOnRule) return;
            _hideOnRule = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 隐藏规则
    /// </summary>
    public Ruleset.Ruleset HidingRules
    {
        get => _hidingRules;
        set
        {
            if (Equals(value, _hidingRules)) return;
            _hidingRules = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 这个组件关联的组件注册信息。
    /// </summary>
    [JsonIgnore]
    public ComponentInfo AssociatedComponentInfo =>
        ComponentRegistryService.Registered.FirstOrDefault(x => string.Equals(x.Guid.ToString(), Id, StringComparison.CurrentCultureIgnoreCase)) ?? ComponentInfo.Empty;
}