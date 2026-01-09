using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Models.Components;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Components;

/// <summary>
/// 代表主界面行的配置
/// </summary>
public partial class MainWindowLineSettings : ObservableObject, IMainWindowCustomizableNodeSettings
{
    /// <summary>
    /// 主界面行包含的组件列表
    /// </summary>
    [ObservableProperty] private ObservableCollection<ComponentSettings> _children = [];

    /// <summary>
    /// 是否为主要行
    /// </summary>
    [ObservableProperty] private bool _isMainLine = false;

    /// <summary>
    /// 是否启用提醒
    /// </summary>
    [ObservableProperty] private bool _isNotificationEnabled = true;
    
    [ObservableProperty] private bool _isResourceOverridingEnabled;
    [ObservableProperty] private double _mainWindowSecondaryFontSize = 14;
    [ObservableProperty] private double _mainWindowBodyFontSize = 16;
    [ObservableProperty] private double _mainWindowEmphasizedFontSize = 18;
    [ObservableProperty] private double _mainWindowLargeFontSize = 20;
    [ObservableProperty] private bool _isCustomForegroundColorEnabled;
    [ObservableProperty] private Color _foregroundColor = Colors.DodgerBlue;
    [ObservableProperty] private double _backgroundOpacity = 0.5;
    [ObservableProperty] private bool _isCustomBackgroundOpacityEnabled;
    [ObservableProperty] private Color _backgroundColor = Colors.Black;
    [ObservableProperty] private bool _isCustomBackgroundColorEnabled;
    [ObservableProperty] private double _customCornerRadius;
    [ObservableProperty] private bool _isCustomCornerRadiusEnabled;
    [ObservableProperty] private double _opacity = 1.0;
    
    private bool _isVisible = true;
    private bool _hideOnRule = false;
    private Ruleset.Ruleset _hidingRules = new();
    /// <summary>
    /// 行当前是否可见
    /// </summary>
    [JsonIgnore]
    public bool IsVisible
    {
        get => _isVisible;
        internal set
        {
            if (value == _isVisible) return;
            _isVisible = value;
            OnPropertyChanged();
        }
    }
    /// <summary>
    /// 0 - 默认;1 - 禁用;2 - 启用
    /// </summary>
    [ObservableProperty] private int _islandSeparationMode = 0;
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
}