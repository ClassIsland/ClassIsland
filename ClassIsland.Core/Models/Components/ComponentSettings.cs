using System.Text.Json.Serialization;
using System.Windows.Media;
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
    private double _mainWindowSecondaryFontSize = 14;
    private double _mainWindowBodyFontSize = 16;
    private double _mainWindowEmphasizedFontSize = 18;
    private double _mainWindowLargeFontSize = 20;
    private bool _isResourceOverridingEnabled = false;
    private Color _foregroundColor = Colors.DodgerBlue;
    private bool _isCustomForegroundColorEnabled = false;

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

    #region Resources

    /// <summary>
    /// 是否启用资源覆盖
    /// </summary>
    public bool IsResourceOverridingEnabled
    {
        get => _isResourceOverridingEnabled;
        set
        {
            if (value == _isResourceOverridingEnabled) return;
            _isResourceOverridingEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 次级字体大小
    /// </summary>
    public double MainWindowSecondaryFontSize
    {
        get => _mainWindowSecondaryFontSize;
        set
        {
            if (value.Equals(_mainWindowSecondaryFontSize)) return;
            _mainWindowSecondaryFontSize = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 正文字体大小
    /// </summary>
    public double MainWindowBodyFontSize
    {
        get => _mainWindowBodyFontSize;
        set
        {
            if (value.Equals(_mainWindowBodyFontSize)) return;
            _mainWindowBodyFontSize = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 强调字体大小
    /// </summary>
    public double MainWindowEmphasizedFontSize
    {
        get => _mainWindowEmphasizedFontSize;
        set
        {
            if (value.Equals(_mainWindowEmphasizedFontSize)) return;
            _mainWindowEmphasizedFontSize = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 大号字体大小
    /// </summary>
    public double MainWindowLargeFontSize
    {
        get => _mainWindowLargeFontSize;
        set
        {
            if (value.Equals(_mainWindowLargeFontSize)) return;
            _mainWindowLargeFontSize = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否启用自定义前景色
    /// </summary>
    public bool IsCustomForegroundColorEnabled
    {
        get => _isCustomForegroundColorEnabled;
        set
        {
            if (value == _isCustomForegroundColorEnabled) return;
            _isCustomForegroundColorEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 自定义前景色
    /// </summary>
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            if (Nullable.Equals(value, _foregroundColor)) return;
            _foregroundColor = value;
            OnPropertyChanged();
        }
    }

    #endregion
}