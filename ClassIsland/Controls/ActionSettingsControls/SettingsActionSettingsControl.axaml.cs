using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Models;
using ClassIsland.Models.Actions;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using static ClassIsland.Services.Automation.Actions.SettingsAction;
namespace ClassIsland.Controls.ActionSettingsControls;

public partial class SettingsActionSettingsControl : ActionSettingsControlBase<SettingsActionSettings>
{
    public SettingsActionSettingsControl()
    {
        InitializeComponent();
        ViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.Kind))
                NotifyKindChanged();
        };
    }

    SettingsService SettingsService { get; } = App.GetService<SettingsService>();
    public SettingsActionSettingsControlViewModel ViewModel { get; } = new();
    protected override void OnAdded() => OpenSelectorDialog();


    void PropertyNameTextBox_OnTextChanged(object? sender, TextChangedEventArgs e) => UpdateSuggestions();
    void FileSelectorButton_OnClick(object? sender, RoutedEventArgs e) => OpenSelectorDialog();
    void FillCurrentValueButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var prop = SettingsService.GetPropertyInfoByName(Settings.Name);
            Settings.Value = Serialize(prop.GetValue(SettingsService.Settings), prop.PropertyType);
        }
        catch
        {
        }
    }


    void OpenSelectorDialog()
    {
        _settingsProperties = GetSettingsProperties();
        UpdateSearchResults();

        if (this.FindResource("SettingsActionSettingsDrawer") is not Control cc) return;
        cc.DataContext = this;
        _ = ShowDrawer(cc);
    }

#region 自动建议

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UpdateSuggestions();
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        SettingsService.Settings.PropertyChanged -= SettingsOnPropertyChanged;
    }

    void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == Settings.Name) UpdateSuggestions();
    }

    void UpdateSuggestions()
    {
        try
        {
            ViewModel.Pack = PackProperty(SettingsService.GetPropertyInfoByName(Settings.Name))
                             ?? throw new KeyNotFoundException();
            var type = ViewModel.Pack.Type;

            try
            {
                Deserialize(Settings.Value, type);
            }
            catch
            {
                Settings.Value = ViewModel.Pack.Value;
            }

            if (ViewModel.Pack.Info?.Enums != null)
            {
                ViewModel.Kind = "enums";
            }
            else if (type.IsEnum)
            {
                ViewModel.Kind = "enums";
                ViewModel.Pack.Info.Enums = Enum.GetNames(type);
            }
            else if (type == typeof(bool))
                ViewModel.Kind = "bool";
            else if (type == typeof(int))
                ViewModel.Kind = "int";
            else if (type == typeof(double))
                ViewModel.Kind = "double";
            else if (type == typeof(Color))
                ViewModel.Kind = "color";
            else
                ViewModel.Kind = "normal";

            return;
        }
        catch (KeyNotFoundException)
        {
        }

        ViewModel.Pack = new();
        ViewModel.Kind = "normal";
    }

    void NotifyKindChanged()
    {
        var cur = SettingsActionKindContentPresenter.ContentTemplate;
        SettingsActionKindContentPresenter.ContentTemplate = null;
        SettingsActionKindContentPresenter.ContentTemplate = cur;
    }

#endregion

#region 搜索

    void SearchSettingsTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        GlobalSearchSettingsTextBox = (TextBox)sender!;
        UpdateSearchResults();
    }

    TextBox? GlobalSearchSettingsTextBox;

    List<SettingsPropertyDetail> _settingsProperties;

    void UpdateSearchResults()
    {
        var kw = GlobalSearchSettingsTextBox?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(kw))
        {
            ViewModel.SettingsSearchResults = _settingsProperties.Where(item =>
                EasyTypes.Contains(item.Type)).ToList();
        }
        else
        {
            ViewModel.SettingsSearchResults = _settingsProperties.Where(item =>
                item.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                item.Info?.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) == true ||
                item.Info?.Note?.Contains(kw, StringComparison.OrdinalIgnoreCase) == true ||
                item.Type.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                item.Value.Contains(kw, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }
    }

    /// <summary>
    /// 获取Settings类中所有属性的详细信息列表
    /// </summary>
    /// <returns>包含属性信息的列表</returns>
    List<SettingsPropertyDetail> GetSettingsProperties() =>
        typeof(Settings)
            .GetProperties(SettingsService.SettingsPropertiesFlags)
            .Where(prop => prop.Name != nameof(Models.Settings.SettingsOverlays) &&
                           prop.GetCustomAttribute<ObsoleteAttribute>() == null)
            .Select(PackProperty)
            .OfType<SettingsPropertyDetail>()
            .OrderBy(item => item.Info?.Name != null ? 0 : 1)
            .ThenBy(item => item.Name)
            .ToList();

    SettingsPropertyDetail? PackProperty(PropertyInfo prop)
    {
        object? value;
        try
        {
            value = prop.GetValue(SettingsService.Settings);
        }
        catch
        {
            return null;
        }

        var currentValue = ToFriendlyValue(value);

        // 获取 SettingsInfo 特性
        var settingsInfo = prop.GetCustomAttribute<SettingsInfo>();

        return new()
        {
            Name = prop.Name,
            Type = prop.PropertyType,
            Value = currentValue,
            Info = settingsInfo
        };
    }

    public static string ToFriendlyValue(object? value)
    {
        switch (value)
        {
            case bool b:
                return b ? "✓" : "✗";
            case Color c:
                return c.ToString();
            default:
            {
                if (value == null || EasyTypes.Contains(value.GetType()))
                    return value?.ToString() ?? "???";
                return JsonSerializer.Serialize(value, FriendlyJsonSerializerOptions);
            }
        }
    }

    /// <summary>
    /// 存储Settings类中属性的详细信息
    /// </summary>
    public class SettingsPropertyDetail
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 属性类型（Type类型）
        /// </summary>
        public Type Type { get; set; } = typeof(object);

        /// <summary>
        /// 属性当前值（友好）
        /// </summary>
        public string Value { get; set; } = "";

        /// <summary>
        /// 属性的SettingsInfo特性（可能为null）
        /// </summary>
        public SettingsInfo? Info { get; set; }
    }

#endregion

    public partial class SettingsActionSettingsControlViewModel : ObservableRecipient
    {
        [ObservableProperty] List<SettingsPropertyDetail> _settingsSearchResults = null!;
        [ObservableProperty] SettingsPropertyDetail _pack = new();
        [ObservableProperty] string _kind = "normal";
    }
}