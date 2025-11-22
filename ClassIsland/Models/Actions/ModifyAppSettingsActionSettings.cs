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
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 存储编辑控件返回的值。
    /// </summary>
    /// 从 config 序列化时，为 JsonElement
    public object? Value { get; set; }
}