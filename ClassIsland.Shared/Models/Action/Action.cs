using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Shared.Models.Action;

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
            Exception = null;
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

    private Exception? _exception;
    /// <summary>
    /// 行动错误。
    /// </summary>
    [JsonIgnore]
    public Exception? Exception
    {
        get => _exception;
        set
        {
            if (Equals(value, _exception)) return;
            _exception = value;
            OnPropertyChanged();
        }
    }

    private bool _isWorking = false;
    /// <summary>
    /// 行动正在运行。
    /// </summary>
    [JsonIgnore]
    public bool IsWorking
    {
        get => _isWorking;
        set
        {
            if (Equals(value, _isWorking)) return;
            _isWorking = value;
            OnPropertyChanged();
        }
    }
}