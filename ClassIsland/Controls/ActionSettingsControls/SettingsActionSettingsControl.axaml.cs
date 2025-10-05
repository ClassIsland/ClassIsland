using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
    public SettingsActionSettingsControl() => InitializeComponent();
    SettingsService SettingsService { get; } = App.GetService<SettingsService>();
    public SettingsActionSettingsControlViewModel ViewModel { get; } = new();
    protected override void OnAdded() => OpenSelectorDialog();


    void PropertyNameTextBox_OnTextChanged(object? sender, TextChangedEventArgs e) => UpdateSuggestions();
    void FileSelectorButton_OnClick(object? sender, RoutedEventArgs e) => OpenSelectorDialog();
    void FillCurrentValueButton_OnClick(object? sender, RoutedEventArgs e) => Settings.Value = ViewModel.Pack.Value;


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
        // UpdateSuggestions();
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
            ViewModel.Pack = PackProperty(SettingsService.GetPropertyInfoByName(Settings.Name));
            var type = ViewModel.Pack.Type;

            try
            {
                Deserialize(Settings.Value, type);
            }
            catch
            {
                Settings.Value = ViewModel.Pack.Value;
            }

            if (ViewModel.Pack?.Info?.Enums != null)
                ViewModel.Kind = "enums";
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
        }
        catch (KeyNotFoundException)
        {
            ViewModel.Pack.Value = "";
            ViewModel.Kind = "normal";
        }
    }

#endregion

#region 搜索

    void SearchSettingsTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        GlobalSearchSettingsTextBox = (TextBox)sender!;
        UpdateSearchResults();
    }

    TextBox GlobalSearchSettingsTextBox;

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
            .Select(PackProperty)
            .OfType<SettingsPropertyDetail>()
            .OrderBy(item => item.Info?.Name != null ? 0 : 1)
            .ThenBy(item => item.Name)
            .ToList();

    SettingsPropertyDetail? PackProperty(PropertyInfo prop)
    {
        object? value = null;
        try
        {
            value = prop.GetValue(SettingsService.Settings);
        }
        catch
        {
        }

        if (value == null) return null;

        var currentValue = Serialize(value, prop.PropertyType);

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

    /// <summary>
    /// 存储Settings类中属性的详细信息
    /// </summary>
    public class SettingsPropertyDetail
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 属性类型（Type类型）
        /// </summary>
        public Type Type { get; set; } = typeof(object);

        /// <summary>
        /// 属性当前值（Json转换后）
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 属性的SettingsInfo特性（可能为null）
        /// </summary>
        public SettingsInfo? Info { get; set; }
    }

#endregion

    public partial class SettingsActionSettingsControlViewModel : ObservableRecipient
    {
        [ObservableProperty] List<SettingsPropertyDetail> _settingsSearchResults;
        [ObservableProperty] SettingsPropertyDetail _pack = new();
        [ObservableProperty] string _kind;
    }
}