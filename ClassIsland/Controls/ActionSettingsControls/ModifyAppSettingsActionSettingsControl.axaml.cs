using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Models.Actions;
using ClassIsland.Services;
using ClassIsland.Services.Automation.Actions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using static ClassIsland.Services.Automation.Actions.ModifyAppSettingsAction;
namespace ClassIsland.Controls.ActionSettingsControls;

/// <summary>
/// 用于配置"修改应用设置"行动的控件。
/// </summary>
///
/// 支持的特性：
/// - SupportedTypes (string, bool, int, double, Color)
/// - 可以转为 Json 的格式 (e.g. Ruleset, List, ...)
/// - 以 int 格式存储，并在 SettingsInfo 中标注了 Enums 的属性 (e.g. 点击托盘图标行为, ...)
/// - 为特定属性自定义控件 (e.g. CurrentComponentConfig)
///
/// 未确认的特性：
/// - Enums 类型
/// - 将可空类型设为非 null 值
///
/// 不支持的特性：
/// - 将可空类型设为 null
///
/// <seealso cref="ModifyAppSettingsAction"/>
public partial class ModifyAppSettingsActionSettingsControl : ActionSettingsControlBase<ModifyAppSettingsActionSettings>
{
    public ModifyAppSettingsActionSettingsControl() => InitializeComponent();

    SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    ILogger<ModifyAppSettingsActionSettingsControl> Logger { get; } =
        App.GetService<ILogger<ModifyAppSettingsActionSettingsControl>>();

    Lazy<IComponentsService> ComponentsService { get; } = new(App.GetService<IComponentsService>);

    ModifyAppSettingsActionSettingsControlViewModel ViewModel { get; } = new();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); // 此时触发 OnAdded()

        if (CurrentPropertyInfo != null)
        {
            UpdateSuggestions();
            if (Settings.Value == null)
                FillCurrentValue();
            if (SetInputValue(Settings.Value))
                UpdateInputer();
        }

        ViewModel.PropertyChanged += ViewModel_OnPropertyChanged;
        Settings.PropertyChanged += ActionSettings_OnPropertyChanged;
    }

    protected override void OnAdded()
    {
        if (Settings.Name == "")
            OpenSelectorDialog();
        else if (Settings.Value == null)
        {
            FillCurrentValue();
            UpdateSuggestions();
            UpdateInputer();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        ViewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
        Settings.PropertyChanged -= ActionSettings_OnPropertyChanged;
    }

    protected override bool IsUndoDeleteRequested() =>
        JsonSerializer.Serialize(Settings.Value, FriendlyJsonSerializerOptions).Length > 15;

    void ActionSettings_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.Name))
        {
            var prevTemplateName = ViewModel.ControlTemplateName;
            var prevType = ViewModel.CurrentSettingsInfo?.Type;
            UpdateSuggestions();
            if (prevTemplateName != ViewModel.ControlTemplateName || ViewModel.ControlTemplateName == ".enums")
            {
                try {
                    ViewModel._inputValueLock = true;
                    ResetInputer();
                    ViewModel._inputValueLock = false;
                    FillCurrentValue();
                    ViewModel._inputValueLock = true;
                    UpdateInputer();
                }
                finally { ViewModel._inputValueLock = null; }
            }
            else if (ViewModel.ControlTemplateName == ".text" && prevType != ViewModel.CurrentSettingsInfo?.Type)
            {
                FillCurrentValue();
            }
        }
    }

    void ViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.InputValue))
        {
            Settings.Value = ViewModel.InputValue;
        }
    }



    SettingsInfo PackSettingsInfo(PropertyInfo property)
    {
        object? value = null;
        try
        {
            value = property.GetValue(SettingsService.Settings);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取属性 {PropertyName} 的值失败", property.Name);
        }

        var settingsInfo = property.GetCustomAttribute<Core.Attributes.SettingsInfo>();
        var type = property.PropertyType;
        var enums = settingsInfo?.Enums ?? (type.IsEnum ? GetEnumNames(GetUnderlyingType(type)) : null);
        return new SettingsInfo(settingsInfo?.Name ?? property.Name, settingsInfo?.Glyph, enums, settingsInfo?.Order ?? 10)
        {
            PropertyName = property.Name,
            Type = type,
            PreviewValue = ConvertToPreviewValue(),
            IsSettingsInfoAttributed = settingsInfo != null
        };

        /// 将数据转换为给前端用户预览的格式。
        string ConvertToPreviewValue() =>
            value switch
            {
                null => "[null!]",
                int index and >= 0 when index < enums?.Length => enums[index],
                double d => d.ToString("0.0#####").TrimEnd('0').TrimEnd('.') + (d % 1 == 0 ? ".0" : ""),
                decimal d => d.ToString("0.0#####").TrimEnd('0').TrimEnd('.') + (d % 1 == 0 ? ".0" : ""),
                bool b => b ? "开" : "关",
                _ => SupportedTypes.Contains(GetUnderlyingType(type))
                    ? value.ToString()!
                    : JsonSerializer.Serialize(value, FriendlyJsonSerializerOptions)
            };

        static string[] GetEnumNames(Type enumType) =>
            Enum.GetValues(enumType)
                .Cast<object>()
                .Select(enumValue =>
                {
                    var fieldInfo = enumType.GetField(enumValue.ToString()!);
                    return fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                        .FirstOrDefault() is DescriptionAttribute attr
                        ? attr.Description
                        : enumValue.ToString()!;
                })
                .ToArray();
    }

    bool IsCustomizedControlTemplateSupported(string name) =>
        ControlTemplateSelector.Templates.ContainsKey(name);

#region 选择应用设置部分

    TextBox? GlobalSearchSettingsTextBox;

    Control? _drawer;
    Control Drawer
    {
        get
        {
            if (_drawer == null)
            {
                _drawer = (Control)this.FindResource("Drawer")!;
                _drawer.DataContext = this;
            }
            return _drawer;
        }
    }

    List<SettingsInfo>? _settingsProperties;
    List<SettingsInfo> SettingsProperties => _settingsProperties ??= typeof(Settings)
        .GetProperties(SettingsService.SettingsPropertiesFlags)
        .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() == null)
        .Select(PackSettingsInfo)
        .OrderBy(item => item.Order)
        .ThenByDescending(item => item.IsSettingsInfoAttributed)
        .ThenBy(item => item.Glyph == null)
        .ThenBy(item => item.Name)
        .ToList();

    void SelectorButton_OnClick(object? sender, RoutedEventArgs e) => OpenSelectorDialog();

    void OpenSelectorDialog()
    {
        UpdateSearchResults();
        _ = ShowDrawer(Drawer);
    }

    void SearchSettingsTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        GlobalSearchSettingsTextBox = (TextBox)sender!;
        UpdateSearchResults();
    }

    bool IsPropertySupported(string name, Type type)
    {
        if (IsCustomizedControlTemplateSupported(name)) return true;
        type = GetUnderlyingType(type);
        return SupportedTypes.Contains(type) || type.IsEnum;
    }

    void UpdateSearchResults()
    {
        var kw = GlobalSearchSettingsTextBox?.Text?.Trim();
        if (string.IsNullOrEmpty(kw))
            ViewModel.SettingsSearchResults = SettingsProperties
                .Where(item => IsPropertySupported(item.PropertyName, item.Type))
                .ToList();
        else
            ViewModel.SettingsSearchResults = SettingsProperties
                .Where(item => MatchesKeyword(item, kw))
                .ToList();

        return;

        static bool MatchesKeyword(SettingsInfo item, string keyword)
        {
            return item.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true ||
                   item.PropertyName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                   item.Type.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                   item.PreviewValue.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }
    }

#endregion

#region 控件模版选择部分

    ModifyAppSettingsActionControlTemplateSelector? _controlTemplateSelector;
    ModifyAppSettingsActionControlTemplateSelector ControlTemplateSelector => _controlTemplateSelector ??=
        (ModifyAppSettingsActionControlTemplateSelector)this.FindResource("ControlTemplateSelector")!;

    void ResetInputer()
    {
        ControlTemplateSelector.ControlTemplateName = "";
        InputerContentPresenter1.Content = InputerContentPresenter2.Content = null;
    }

    void UpdateInputer()
    {
        if (ViewModel.CurrentSettingsInfo?.Type == null) return;

        ResetInputer();

        ControlTemplateSelector.ControlTemplateName = ViewModel.ControlTemplateName;

        ViewModel.IsInContentPresenter2 =
            Settings.Value?.ToString()?.Contains(Environment.NewLine) == true ||
            ViewModel.ControlTemplateName == ".text" &&
            Settings.Value?.ToString()?.Length > 20;

        if (ViewModel.IsInContentPresenter2)
            InputerContentPresenter2.Content = ControlTemplateSelector.Build();
        else
            InputerContentPresenter1.Content = ControlTemplateSelector.Build();
    }

    void UpdateSuggestions()
    {
        if (CurrentPropertyInfo == null) return;
        ViewModel.CurrentSettingsInfo = PackSettingsInfo(CurrentPropertyInfo);
        ViewModel.ControlTemplateName = DetermineControlType(CurrentPropertyInfo.PropertyType);
        return;

        string DetermineControlType(Type type)
        {
            if (IsCustomizedControlTemplateSupported(Settings.Name))
                return Settings.Name;

            if (ViewModel.CurrentSettingsInfo.Enums != null)
                return ".enums";

            if (type == typeof(bool)) return ".bool";
            if (type == typeof(int)) return ".int";
            if (type == typeof(double)) return ".double";
            if (type == typeof(Color)) return ".color";

            return ".text";
        }
    }


#endregion

#region 填充字段部分

    void FillCurrentValueButton_OnClick(object? sender, RoutedEventArgs e) => FillCurrentValue();

    void FillCurrentValue()
    {
        if (CurrentPropertyInfo != null)
        {
            SetInputValue(CurrentPropertyInfo.GetValue(SettingsService.Settings));
        }
    }

    string? _lastName;
    PropertyInfo? _cachedPropertyInfo;
    PropertyInfo? CurrentPropertyInfo
    {
        get
        {
            var currentName = Settings.Name;
            if (currentName != _lastName)
            {
                _cachedPropertyInfo = SettingsService.GetPropertyInfoByName(currentName);
                _lastName = currentName;
            }
            return _cachedPropertyInfo;
        }
    }


    bool SetInputValue(object? value)
    {
        var v = ConvertToEditableType();
        if (v != null)
        {
            Settings.Value = ViewModel.InputValue = v;
            return true;
        }

        return false;

        /// 将数据转换为前端用户可编辑的类型。
        object? ConvertToEditableType()
        {
            try
            {
                if (value == null) return null;

                switch (ViewModel.ControlTemplateName)
                {
                    case ".text":
                        return value switch
                        {
                            string str => str,
                            JsonElement json => json.GetString(),
                            _ => JsonSerializer.Serialize(value, FriendlyJsonSerializerOptions)
                        };
                    case ".enums":
                        return value switch
                        {
                            int i => i,
                            JsonElement json => json.Deserialize<int>(FriendlyJsonSerializerOptions),
                            Enum e => Convert.ToInt32(e),
                            _ => Convert.ToInt32(value)
                        };
                    case ".color":
                        return value switch
                        {
                            Color c => c,
                            string str => JsonSerializer.Deserialize<Color>(str),
                            JsonElement json => json.Deserialize<Color>(),
                            _ => Color.Parse(value.ToString()!)
                        };
                    case ".int":
                        return value switch
                        {
                            int i => i,
                            double d => (int)d,
                            JsonElement { ValueKind: JsonValueKind.Number } json => (int)json.Deserialize<decimal>(),
                            JsonElement { ValueKind: JsonValueKind.String } json => (int)JsonSerializer.Deserialize<decimal>(json.GetString()!),
                            _ => (int)Convert.ToDecimal(value)
                        };
                }

                if (CurrentPropertyInfo == null) return value;

                if (value is JsonElement jsonElement)
                    try
                    {
                        return jsonElement.Deserialize(CurrentPropertyInfo.PropertyType, FriendlyJsonSerializerOptions);
                    }
                    catch (JsonException ex)
                    {
                        Logger.LogError(ex, "尝试对 ({ValueKind}) {} 解析为可编辑类型 {type} 时出错。", jsonElement.ValueKind, jsonElement, CurrentPropertyInfo.PropertyType);
                    }

                if (value.GetType() != CurrentPropertyInfo.PropertyType)
                {
                    return Convert.ChangeType(value, CurrentPropertyInfo.PropertyType);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "尝试对 ({type}) {} 解析为可编辑类型 {template} 模版时出错。", value?.GetType(), value, ViewModel.ControlTemplateName);
            }

            return value;
        }
    }
}

#endregion

public partial class ModifyAppSettingsActionSettingsControlViewModel : ObservableRecipient
{
    internal bool? _inputValueLock = null;
    object _inputValue = "[未初始化]";
    public object InputValue
    {
        get => _inputValue;
        set
        {
            if (value != null && !value.Equals(_inputValue))
            {
                if (_inputValueLock == true)
                {
                    Debug.WriteLine($"ModifyAppSettingsActionSettingsControl：拦截了 InputValue set。({value.GetType().Name}) {value}");
                    return;
                }

                _inputValue = value;

                if (_inputValueLock == false)
                    _inputValueLock = true;

                OnPropertyChanged();
                return;
            }

            if (_inputValueLock == false)
                _inputValueLock = true;
        }
    }

    [ObservableProperty] string _controlTemplateName = null!;
    [ObservableProperty] SettingsInfo? _currentSettingsInfo = null;
    [ObservableProperty] List<SettingsInfo> _settingsSearchResults = null!;
    [ObservableProperty] bool _isInContentPresenter2;
}

public class SettingsInfo(string? name, string? glyph, string[]? enums = null, double order = 10) : Core.Attributes.SettingsInfo(name, glyph, enums, order)
{
    public string PropertyName { get; init; }
    public Type Type { get; init; }
    public string PreviewValue { get; init; }
    public bool IsSettingsInfoAttributed { get; init; }
}