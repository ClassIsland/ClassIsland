﻿using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Models;
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
    private bool _isMinWidthEnabled = false;
    private double _minWidth = 100;
    private bool _isMaxWidthEnabled = false;
    private double _maxWidth = 300;
    private bool _isFixedWidthEnabled = false;
    private double _fixedWidth = 200;
    private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Stretch;
    private int _relativeLineNumber = 0;
    private bool _isVisible = true;
    private double _lastWidthCache = 100;

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
            OnPropertyChanged(nameof(Children));
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

    [JsonIgnore]
    internal bool IsMigrated { get; set; } = false;

    [JsonIgnore] internal Guid MigrationSource { get; set; } = Guid.Empty;

    /// <summary>
    /// 这个组件关联的组件注册信息。
    /// </summary>
    [JsonIgnore]
    public ComponentInfo AssociatedComponentInfo =>
        ComponentRegistryService.Registered.FirstOrDefault(x => string.Equals(x.Guid.ToString(), Id, StringComparison.CurrentCultureIgnoreCase)) ?? ComponentInfo.Empty;

    /// <summary>
    /// 这个组件包含的组件
    /// </summary>
    /// <remarks>
    /// 如果这个组件不是容器组件，或组件设置没有实现<see cref="IComponentContainerSettings"/>，那么此属性将为 null。
    /// </remarks>
    [JsonIgnore]
    public ObservableCollection<ComponentSettings>? Children => (Settings as IComponentContainerSettings)?.Children;

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

    #region Layouts

    /// <summary>
    /// 相对行号
    /// </summary>
    public int RelativeLineNumber
    {
        get => _relativeLineNumber;
        set
        {
            if (value == _relativeLineNumber) return;
            _relativeLineNumber = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否启用最小宽度
    /// </summary>
    public bool IsMinWidthEnabled
    {
        get => _isMinWidthEnabled;
        set
        {
            if (value == _isMinWidthEnabled) return;
            _isMinWidthEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 最小宽度
    /// </summary>
    /// <remarks>
    /// 此属性仅在 <see cref="IsMinWidthEnabled"/> 为 true 时生效。
    /// </remarks>
    public double MinWidth
    {
        get => _minWidth;
        set
        {
            if (value.Equals(_minWidth)) return;
            _minWidth = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否启用最大宽度
    /// </summary>
    public bool IsMaxWidthEnabled
    {
        get => _isMaxWidthEnabled;
        set
        {
            if (value == _isMaxWidthEnabled) return;
            _isMaxWidthEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 最大宽度
    /// </summary>
    /// <remarks>
    /// 此属性仅在 <see cref="IsMaxWidthEnabled"/> 为 true 时生效。
    /// </remarks>
    public double MaxWidth
    {
        get => _maxWidth;
        set
        {
            if (value.Equals(_maxWidth)) return;
            _maxWidth = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否启用固定宽度
    /// </summary>
    public bool IsFixedWidthEnabled
    {
        get => _isFixedWidthEnabled;
        set
        {
            if (value == _isFixedWidthEnabled) return;
            _isFixedWidthEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 固定宽度
    /// </summary>
    /// <remarks>
    /// 此属性仅在 <see cref="IsFixedWidthEnabled"/> 为 true 时生效。
    /// </remarks>
    public double FixedWidth
    {
        get => _fixedWidth;
        set
        {
            if (value.Equals(_fixedWidth)) return;
            _fixedWidth = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 组件水平对齐方式
    /// </summary>
    public HorizontalAlignment HorizontalAlignment
    {
        get => _horizontalAlignment;
        set
        {
            if (value == _horizontalAlignment) return;
            _horizontalAlignment = value;
            OnPropertyChanged();
        }
    }

    #endregion

    /// <summary>
    /// 组件当前是否可见
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
    /// 组件上次的宽度缓存。
    /// </summary>
    public double LastWidthCache
    {
        get => _lastWidthCache;
        set
        {
            if (value.Equals(_lastWidthCache)) return;
            _lastWidthCache = value;
            OnPropertyChanged();
        }
    }
}