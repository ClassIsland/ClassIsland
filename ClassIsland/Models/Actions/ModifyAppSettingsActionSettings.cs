using System;
using System.Diagnostics.Contracts;
using System.Text.Json;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Models.Actions;

/// <summary>
/// "修改应用设置"行动设置。
/// </summary>
public class ModifyAppSettingsActionSettings : ObservableRecipient
{
    string _name = "";
    public string Name
    {
        get => _name;
        set
        {
            var v = value.Trim();
            if (v == _name) return;
            _name = v;
            OnPropertyChanged();
        }
    }

    object _value = "";
    public object Value
    {
        get
        {
            var info = typeof(Settings).GetProperty(Name, SettingsService.SettingsPropertiesFlags);
            if (info == null || _value.GetType() == info.PropertyType) return _value;

            if (_value is not JsonElement json)
                return ToTargetType(_value, info.PropertyType) ?? _value;

            return json.Deserialize(info.PropertyType) ?? _value;
        }
        set
        {
            if (value == null || value.Equals(_value)) return;
            _value = value;
            // OnPropertyChanged();
        }
    }

    [Pure] static object? ToTargetType(object value, Type type)
    {
        var str = value.ToString();
        if (str == null) return null;

        if (type == typeof(string))
            return value;

        try
        {
            if (type == typeof(int))
                return (int)double.Parse(str);

            if (type == typeof(double))
                return double.Parse(str);

            if (type == typeof(bool))
                return bool.Parse(str);

            return JsonSerializer.Deserialize(str, type);
        }
        catch
        {
            return null;
        }
    }
}