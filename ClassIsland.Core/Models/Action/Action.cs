using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Core.Models.Action;

/// <summary>
/// 代表一个行动。
/// </summary>
public class Action : ObservableRecipient
{
    private string _id = "";
    /// <summary>
    /// 行动 ID。
    /// </summary>
    public string Id
    {
        get => _id;
        set
        {
            if (value == _id) return;
            _id = value;
            OnPropertyChanged();
        }
    }

    private object? _settings;
    /// <summary>
    /// 行动设置。
    /// </summary>
    public object? Settings
    {
        get => _settings;
        set
        {
            if (Equals(value, _settings)) return;
            _settings = value;
            OnPropertyChanged();
        }
    }
}