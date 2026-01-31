using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 可附加设置的控件
/// </summary>
public abstract class AttachedSettingsControlBase : UserControl, INotifyPropertyChanged
{
    public static readonly StyledProperty<AttachedSettingsTargets> TargetProperty = AvaloniaProperty.Register<AttachedSettingsControlBase, AttachedSettingsTargets>(
        nameof(Target));

    /// <summary>
    /// 该设置附加到的对象类型
    /// </summary>
    public AttachedSettingsTargets Target
    {
        get => GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public static readonly StyledProperty<int> AttachedTimePointTimeTypeProperty = AvaloniaProperty.Register<AttachedSettingsControlBase, int>(
        nameof(AttachedTimePointTimeType), -1);

    /// <summary>
    /// 该设置附加到的时间点的时间类型
    /// </summary>
    public int AttachedTimePointTimeType
    {
        get => GetValue(AttachedTimePointTimeTypeProperty);
        set => SetValue(AttachedTimePointTimeTypeProperty, value);
    }
    
    [NotNull]
    internal object? SettingsInternal { get; set; }

    /// <summary>
    /// 从设置对象获取控件实例。
    /// </summary>
    /// <param name="info">控件信息</param>
    /// <param name="settings">要附加的设置对象</param>
    /// <returns>初始化的控件对象。</returns>
    public static AttachedSettingsControlBase? GetInstance(AttachedSettingsControlInfo info, ref object? settings)
    {
        var control = IAppHost.Host?.Services.GetKeyedService<AttachedSettingsControlBase>(info.Guid);
        if (control == null)
        {
            return null;
        }

        var baseType = info.AttachedSettingsControlType.BaseType;
        if (baseType?.GetGenericArguments().Length > 0)
        {
            var settingsType = baseType.GetGenericArguments().First();
            var settingsReal = settings ?? Activator.CreateInstance(settingsType);
            if (settingsReal is JsonElement json)
            {
                settingsReal = json.Deserialize(settingsType);
            }
            settings = settingsReal;

            control.SettingsInternal = settingsReal;
        }
        return control;
    }

    #region PropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}

/// <summary>
/// 可附加设置的控件
/// </summary>
public abstract class AttachedSettingsControlBase<T> : AttachedSettingsControlBase where T : class
{
    /// <summary>
    /// 当前控件的设置
    /// </summary>
    public T Settings => (SettingsInternal as T)!;
}
