using System.Collections.ObjectModel;
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
    [ObservableProperty] private double _mainWindowSecondaryFontSize;
    [ObservableProperty] private double _mainWindowBodyFontSize;
    [ObservableProperty] private double _mainWindowEmphasizedFontSize;
    [ObservableProperty] private double _mainWindowLargeFontSize;
    [ObservableProperty] private bool _isCustomForegroundColorEnabled;
    [ObservableProperty] private Color _foregroundColor = Colors.DodgerBlue;
    [ObservableProperty] private double _backgroundOpacity = 0.5;
    [ObservableProperty] private bool _isCustomBackgroundOpacityEnabled;
    [ObservableProperty] private Color _backgroundColor = Colors.Black;
    [ObservableProperty] private bool _isCustomBackgroundColorEnabled;
    [ObservableProperty] private double _customCornerRadius;
    [ObservableProperty] private bool _isCustomCornerRadiusEnabled;
    [ObservableProperty] private double _opacity = 1.0;

    /// <summary>
    /// 0 - 默认;1 - 禁用;2 - 启用
    /// </summary>
    [ObservableProperty] private int _islandSeparationMode = 0;
}