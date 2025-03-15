using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Ruleset;

/// <summary>
/// 代表一个规则条目。
/// </summary>
public class Rule : ObservableRecipient
{
    private bool _isReversed = false;
    private string _id = "";
    private object? _settings;
    private int _state = 0;

    /// <summary>
    /// 是否反转判断。
    /// </summary>
    public bool IsReversed
    {
        get => _isReversed;
        set
        {
            if (value == _isReversed) return;
            _isReversed = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 规则 ID。
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

    /// <summary>
    /// 规则集设置。
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

    /// <summary>
    /// 满足状态
    /// </summary>
    [JsonIgnore]
    public int State
    {
        get => _state;
        set
        {
            if (value == _state) return;
            _state = value;
            OnPropertyChanged();
        }
    }
}