using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Models.Actions;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using static ClassIsland.Services.Automation.Actions.ModifyAppSettingsAction;
namespace ClassIsland.Controls.ActionSettingsControls;

/// <summary>
/// 用于配置"修改应用设置"行动的控件。
/// </summary>
public partial class ModifyAppSettingsActionSettingsControl : ActionSettingsControlBase<ModifyAppSettingsActionSettings>
{
    public ModifyAppSettingsActionSettingsControl() => InitializeComponent();

    SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    ILogger<ModifyAppSettingsActionSettingsControl> Logger { get; } =
        App.GetService<ILogger<ModifyAppSettingsActionSettingsControl>>();

    Lazy<IComponentsService> ComponentsService { get; } = new(App.GetService<IComponentsService>);

    ModifyAppSettingsActionSettingsControlViewModel ViewModel { get; } = new();

    Control? _drawer;
    ContentPresenter? _controlTypeContentPresenter;
    List<SettingsInfo>? _settingsProperties;
    ModifyAppSettingsActionControlTemplateSelector? _controlTemplateSelector;

    Control Drawer
    {
        get
        {
            if (_drawer != null) return _drawer;
            _drawer = (Control)this.FindResource("ModifyAppSettingsAction_SettingsDrawer")!;
            _drawer.DataContext = this;
            return _drawer;
        }
    }

    ModifyAppSettingsActionControlTemplateSelector ControlTemplateSelector => _controlTemplateSelector ??=
        (ModifyAppSettingsActionControlTemplateSelector)ControlTypeContentPresenter.ContentTemplate!;

    ContentPresenter ControlTypeContentPresenter => _controlTypeContentPresenter ??=
        (ContentPresenter)this.FindResource("ModifyAppSettingsAction_ControlTemplateContentPresenter")!;

    TextBox? GlobalSearchSettingsTextBox;

    List<SettingsInfo> SettingsProperties => _settingsProperties ??= GetSettingsProperties()
        .OrderByDescending(item => item.IsSettingsInfoAttributed)
        .ThenBy(item => item.Glyph == null)
        .ThenBy(item => item.Name)
        .ToList();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        UpdateSuggestions();
        UpdateInputer();

        var property = SettingsService.GetPropertyInfoByName(Settings.Name);
        if (property != null)
            ViewModel.InputValue = ToInputValue(Settings.Name, Settings.Value, property.PropertyType);

        ViewModel.PropertyChanged += ViewModel_OnPropertyChanged;
        Settings.PropertyChanged += ActionSettings_OnPropertyChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        ViewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
        Settings.PropertyChanged -= ActionSettings_OnPropertyChanged;
    }

    protected override void OnAdded()
    {
        base.OnAdded();
        if (Settings.Name == "")
            OpenSelectorDialog();
        else
            FillCurrentValue();
    }

    protected override bool IsUndoDeleteRequested()
    {
        return JsonSerializer.Serialize(Settings.Value).Length > 10;
    }

    void ActionSettings_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.Name)) UpdateSuggestions();
    }

    void ViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.ControlTemplateName))
        {
            UpdateInputer();
            FillCurrentValue();
        }
        else if (e.PropertyName == nameof(ViewModel.InputValue))
        {
            // if (IsPropertySupported(Settings.Name, ViewModel.CurrentSettingsInfo?.Type))
                Settings.Value = ViewModel.InputValue;
            // else
            // {
            //     Settings.Value = JsonSerializer.Deserialize(ViewModel.InputValue.ToString()!,
            //         ViewModel.CurrentSettingsInfo?.Type!)!;
            // }
        }
    }

    [Pure] bool IsPropertySupported(string name, Type? type)
    {
        if (IsCustomizedControlTemplateSupported(name)) return true;

        if (type == null) return false;

        type = GetUnderlyingType(type);

        if (EasyTypes.Contains(type) || type == typeof(Color))
            return true;

        return type.IsEnum;
    }

    void SelectorButton_OnClick(object? sender, RoutedEventArgs e) => OpenSelectorDialog();
    void FillCurrentValueButton_OnClick(object? sender, RoutedEventArgs e) => FillCurrentValue();

    void SearchSettingsTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        GlobalSearchSettingsTextBox = (TextBox)sender!;
        UpdateSearchResults();
    }

    void OpenSelectorDialog()
    {
        UpdateSearchResults();
        _ = ShowDrawer(Drawer);
    }

    void UpdateInputer()
    {
        InputerContentPresenter1.Content = null;
        InputerContentPresenter2.Content = null;
        ViewModel.IsInInputerContentPresenter2 =
            !IsPropertySupported(Settings.Name, ViewModel.CurrentSettingsInfo?.Type) ||
            ViewModel.CurrentSettingsInfo?.Type == typeof(string) && (Settings.Value as string)?.Length > 20;
        if (ViewModel.IsInInputerContentPresenter2)
            InputerContentPresenter2.Content = ControlTypeContentPresenter;
        else
            InputerContentPresenter1.Content = ControlTypeContentPresenter;
    }

    void UpdateSearchResults()
    {
        var kw = GlobalSearchSettingsTextBox?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(kw))
            ViewModel.SettingsSearchResults = SettingsProperties
                .Where(item => IsPropertySupported(item.PropertyName, item.Type))
                .ToList();
        else
            ViewModel.SettingsSearchResults = SettingsProperties
                .Where(item => MatchesKeyword(item, kw))
                .ToList();
    }

    void UpdateSuggestions()
    {
        var property = SettingsService.GetPropertyInfoByName(Settings.Name);
        if (property == null) return;
        var detail = PackPropertyIntoSettingsPropertyDetail(property);
        if (detail == null) return;
        ViewModel.CurrentSettingsInfo = detail;
        ViewModel.ControlTemplateName = DetermineControlType(property.PropertyType, detail.Enums, Settings.Name);
        if (ViewModel.ControlTemplateName == "enums")
        {
            FillCurrentValue();
        }
    }

    void FillCurrentValue()
    {
        var property = SettingsService.GetPropertyInfoByName(Settings.Name);
        if (property == null) return;
        var value = property.GetValue(SettingsService.Settings);
        ViewModel.InputValue = ToInputValue(Settings.Name, value, property.PropertyType);
    }

    [Pure]
    object ToInputValue(string name, object? value, Type type) => IsPropertySupported(name, type)
        ? value ?? "null"
        : JsonSerializer.Serialize(value, type, FriendlyJsonSerializerOptions);

    [Pure]
    List<SettingsInfo> GetSettingsProperties()
    {
        return typeof(Settings)
            .GetProperties(SettingsService.SettingsPropertiesFlags)
            .Select(PackPropertyIntoSettingsPropertyDetail)
            .OfType<SettingsInfo>()
            .OrderByDescending(item => item.IsSettingsInfoAttributed)
            .ThenBy(item => item.Name)
            .ToList();
    }

    [Pure]
    SettingsInfo? PackPropertyIntoSettingsPropertyDetail(PropertyInfo property)
    {
        object? value;
        try
        {
            value = property.GetValue(SettingsService.Settings);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取属性{PropertyName}的值失败", property.Name);
            return null;
        }

        var settingsInfo = property.GetCustomAttribute<Core.Attributes.SettingsInfo>();
        var type = property.PropertyType;
        var enums = settingsInfo?.Enums ?? (type.IsEnum ? Enum.GetNames(GetUnderlyingType(type)) : null);
        return new SettingsInfo(settingsInfo?.Name ?? property.Name, settingsInfo?.Glyph, enums)
        {
            PropertyName = property.Name,
            Type = type,
            FriendlyValue = GetFriendlyValue(value, type, enums),
            IsSettingsInfoAttributed = settingsInfo != null
        };
    }

    [Pure]
    static bool MatchesKeyword(SettingsInfo item, string keyword)
    {
        return item.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true ||
               item.PropertyName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
               item.Type.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
               item.FriendlyValue.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    [Pure]
    static string GetFriendlyValue(object? value, Type type, string[]? enums)
    {
        return value switch
        {
            null => "null",
            int index and >= 0 when index < enums?.Length => enums[index],
            Color color => color.ToString(),
            _ => EasyTypes.Contains(GetUnderlyingType(type))
                ? value.ToString()!
                : JsonSerializer.Serialize(value, FriendlyJsonSerializerOptions)
        };
    }


    [Pure]
    string DetermineControlType(Type type, string[]? enums, string name)
    {
        if (IsCustomizedControlTemplateSupported(name)) return name;
        return enums == null
            ? TypeToControlType.GetValueOrDefault(type, "normal")
            : "enums";
    }

    static readonly Dictionary<Type, string> TypeToControlType = new()
    {
        { typeof(bool), "bool" },
        { typeof(int), "int" },
        { typeof(double), "double" },
        { typeof(Color), "color" }
    };

    [Pure] bool IsCustomizedControlTemplateSupported(string name) =>
        ControlTemplateSelector.Templates.ContainsKey(name);

    public partial class ModifyAppSettingsActionSettingsControlViewModel : ObservableRecipient
    {
        [ObservableProperty] string _controlTemplateName = null!;
        [ObservableProperty] SettingsInfo? _currentSettingsInfo = null;
        [ObservableProperty] List<SettingsInfo> _settingsSearchResults = null!;
        [ObservableProperty] bool _isInInputerContentPresenter2;

        object? _inputValue;
        [NotNull] public object? InputValue
        {
            get => _inputValue!;
            set
            {
                if (value == null) return;
                _inputValue = value;
                OnPropertyChanged();
            }
        }
    }

    public class SettingsInfo(string? name, string? glyph, string[]? enums = null) : Core.Attributes.SettingsInfo(name, glyph, enums)
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string PropertyName { get; init; } = string.Empty;

        /// <summary>
        /// 属性类型
        /// </summary>
        public Type Type { get; init; } = typeof(object);

        /// <summary>
        /// 友好显示的值
        /// </summary>
        public string FriendlyValue { get; init; } = string.Empty;

        /// <summary>
        /// 是否有<see cref="Core.Attributes.SettingsInfo" />特性
        /// </summary>
        public bool IsSettingsInfoAttributed { get; init; }
    }
}