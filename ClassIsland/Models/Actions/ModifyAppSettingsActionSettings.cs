using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Data;
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
            if (_value is not JsonElement json) return _value;

            var info = typeof(Settings).GetProperty(Name, SettingsService.SettingsPropertiesFlags);
            if (info == null) return _value;

            object? s = null;
            try
            {
                s = json.Deserialize(info.PropertyType);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine(ex);
            }

            return s ?? _value;
        }
        set
        {
            if (value == null || value.Equals(_value)) return;
            _value = value;
            OnPropertyChanged();
        }
    }
}