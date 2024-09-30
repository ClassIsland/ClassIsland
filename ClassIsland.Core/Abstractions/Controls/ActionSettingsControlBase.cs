﻿using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Controls;

using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models;
using ClassIsland.Core.Models.Action;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 可附加设置的控件
/// </summary>
public abstract class ActionSettingsControlBase : UserControl, INotifyPropertyChanged
{
    [NotNull] internal object? SettingsInternal { get; set; }

    /// <summary>
    /// 从设置对象获取控件实例。
    /// </summary>
    /// <param name="info">控件信息</param>
    /// <param name="settings">要附加的设置对象</param>
    /// <returns>初始化的控件对象。</returns>
    public static ActionSettingsControlBase? GetInstance(ActionRegistryInfo info, ref object? settings)
    {
        var control = IAppHost.Host?.Services.GetKeyedService<ActionSettingsControlBase>(info.Id);
        if (control == null || info.SettingsControlType == null)
        {
            return null;
        }

        var baseType = info.SettingsControlType.BaseType;
        if (baseType?.GetGenericArguments().Length > 0)
        {
            var settingsType = baseType.GetGenericArguments().First();
            var settingsReal = settings ?? Activator.CreateInstance(settingsType);
            if (settingsReal is JsonElement json)
            {
                settingsReal = json.Deserialize(settingsType);
            }

            if (settingsReal?.GetType() != settingsType)
            {
                settingsReal = Activator.CreateInstance(settingsType);
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
public abstract class ActionSettingsControlBase<T> : ActionSettingsControlBase where T : class
{
    /// <summary>
    /// 当前控件的设置
    /// </summary>
    public T Settings => (SettingsInternal as T)!;
}
