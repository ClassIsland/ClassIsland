using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Models.Actions;

/// <summary>
/// "应用设置"行动设置。
/// </summary>
public class SettingsActionSettings : ObservableRecipient
{
    string _name = "";
    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    string _value = "";
    public string Value
    {
        get => _value;
        set
        {
            if (value == _value) return;
            _value = value;
            OnPropertyChanged();
        }
    }

    SettingsActionMode _mode = SettingsActionMode.Set;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public SettingsActionMode Mode
    {
        get => _mode;
        set
        {
            if (value == _mode) return;
            _mode = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// "应用设置"行动设置修改模式。
    /// </summary>
    public enum SettingsActionMode
    {
        /// <summary>
        /// 设定
        /// </summary>
        Set,

        /// <summary>
        /// 加
        /// </summary>
        Add,

        /// <summary>
        /// 乘
        /// </summary>
        Multiply,
    }
}