using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
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
    static readonly Dictionary<Type, string> TypeToControlType = new()
    {
        { typeof(bool), "bool" },
        { typeof(int), "int" },
        { typeof(double), "double" },
        { typeof(Color), "color" }
    };

    Control? _controlTypeContentPresenter;
    Control? _drawer;
    List<SettingsPropertyDetail>? _settingsProperties;
    TextBox? GlobalSearchSettingsTextBox;

    public ModifyAppSettingsActionSettingsControl()
    {
        InitializeComponent();
    }

    SettingsService SettingsService { get; } = App.GetService<SettingsService>();


    readonly Lazy<IComponentsService> _componentsService = new(App.GetService<IComponentsService>);
    public IComponentsService ComponentsService => _componentsService.Value;


    ILogger<ModifyAppSettingsActionSettingsControl> Logger { get; } =
        App.GetService<ILogger<ModifyAppSettingsActionSettingsControl>>();

    ModifyAppSettingsActionSettingsControlViewModel ViewModel { get; } = new();

    Control Drawer
    {
        get
        {
            if (_drawer != null) return _drawer;

            _drawer ??= (Control)this.FindResource("ModifyAppSettingsActionSettingsDrawer");
            _drawer.DataContext = this;
            return _drawer;
        }
    }

    Control ControlTypeContentPresenter => _controlTypeContentPresenter ??=
        (Control)this.FindResource("ModifyAppSettingsActionControlTypeContentPresenter");

    List<SettingsPropertyDetail> SettingsProperties => _settingsProperties ??= GetSettingsProperties()
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
            ViewModel.InputValue = ToInputValue(Settings.Value, property.PropertyType);

        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        Settings.PropertyChanged += ActionSettingsOnPropertyChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        Settings.PropertyChanged -= ActionSettingsOnPropertyChanged;
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

    void ActionSettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.Name)) UpdateSuggestions();
    }

    void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.ControlType))
        {
            UpdateInputer();
            FillCurrentValue();
        }
        else if (e.PropertyName == nameof(ViewModel.InputValue))
        {
            if (IsTypeSupported(ViewModel.CurrentSettingsPropertyDetail.Type))
                Settings.Value = ViewModel.InputValue;
            else
            {
                try
                {
                    Settings.Value = JsonSerializer.Deserialize(ViewModel.InputValue.ToString(),
                        ViewModel.CurrentSettingsPropertyDetail.Type);
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
    }

    void SelectorButton_OnClick(object? sender, RoutedEventArgs e) => OpenSelectorDialog();
    void FillCurrentValueButton_OnClick(object? sender, RoutedEventArgs e) => FillCurrentValue();

    void SearchSettingsTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        GlobalSearchSettingsTextBox = (TextBox)sender;
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
            !IsTypeSupported(ViewModel.CurrentSettingsPropertyDetail?.Type) ||
            ViewModel.CurrentSettingsPropertyDetail?.Type == typeof(string) && (Settings.Value as string)?.Length > 20;
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
                .Where(item => IsTypeSupported(item.Type))
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
        ViewModel.CurrentSettingsPropertyDetail = detail;
        ViewModel.ControlType = DetermineControlType(property.PropertyType, detail.Enums, Settings.Name);
        if (ViewModel.ControlType == "enums")
        {
            FillCurrentValue();
        }
    }

    void FillCurrentValue()
    {
        var property = SettingsService.GetPropertyInfoByName(Settings.Name);
        if (property == null) return;
        var value = property.GetValue(SettingsService.Settings);
        ViewModel.InputValue = ToInputValue(value, property.PropertyType);
    }

    [Pure]
    static object ToInputValue(object value, Type type) => IsTypeSupported(type)
        ? value
        : JsonSerializer.Serialize(value, type, FriendlyJsonSerializerOptions);

    [Pure]
    List<SettingsPropertyDetail> GetSettingsProperties()
    {
        return typeof(Settings)
            .GetProperties(SettingsService.SettingsPropertiesFlags)
            .Select(PackPropertyIntoSettingsPropertyDetail)
            .OfType<SettingsPropertyDetail>()
            .OrderByDescending(item => item.IsSettingsInfoAttributed)
            .ThenBy(item => item.Name)
            .ToList();
    }

    [Pure]
    SettingsPropertyDetail? PackPropertyIntoSettingsPropertyDetail(PropertyInfo property)
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

        var settingsInfo = property.GetCustomAttribute<SettingsInfo>();
        var type = property.PropertyType;
        var enums = settingsInfo?.Enums ?? (type.IsEnum ? Enum.GetNames(GetUnderlyingType(type)) : null);
        return new SettingsPropertyDetail(settingsInfo?.Name ?? property.Name, settingsInfo?.Glyph, enums)
        {
            PropertyName = property.Name,
            Type = property.PropertyType,
            FriendlyValue = GetFriendlyValue(value, type, enums),
            IsSettingsInfoAttributed = settingsInfo != null
        };
    }

    [Pure]
    static bool MatchesKeyword(SettingsPropertyDetail item, string keyword)
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
                ? value.ToString()
                : JsonSerializer.Serialize(value, FriendlyJsonSerializerOptions)
        };
    }

    ModifyAppSettingsActionKindTemplateSelector? _kindTemplateSelector;

    ModifyAppSettingsActionKindTemplateSelector KindTemplateSelector
    {
        get
        {
            if (_kindTemplateSelector != null) return _kindTemplateSelector;

            if (Resources.TryGetValue("ModifyAppSettingsActionControlTypeContentPresenter", out var resource) &&
                resource is ContentPresenter contentPresenter)
            {
                _kindTemplateSelector = contentPresenter.ContentTemplate as ModifyAppSettingsActionKindTemplateSelector;
            }

            return _kindTemplateSelector;
        }
    }


    [Pure]
    string DetermineControlType(Type type, string[]? enums, string name)
    {
        if (KindTemplateSelector.Templates.ContainsKey(name))
            return name;
        return enums == null
            ? TypeToControlType.GetValueOrDefault(type, "normal")
            : "enums";
    }

    public partial class ModifyAppSettingsActionSettingsControlViewModel : ObservableRecipient
    {
        [ObservableProperty] string _controlType;
        [ObservableProperty] SettingsPropertyDetail _currentSettingsPropertyDetail;
        [ObservableProperty] List<SettingsPropertyDetail> _settingsSearchResults;
        [ObservableProperty] bool _isInInputerContentPresenter2;

        object _inputValue;

        public object InputValue
        {
            get => _inputValue;
            set
            {
                if (value == null) return;
                _inputValue = value;
                OnPropertyChanged();
            }
        }
    }

    public class SettingsPropertyDetail(string? name, string? glyph, string[]? enums = null) : SettingsInfo(name, glyph, enums)
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
        /// 是否有<see cref="SettingsInfo" />特性
        /// </summary>
        public bool IsSettingsInfoAttributed { get; init; }
    }
}