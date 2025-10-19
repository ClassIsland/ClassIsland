using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Actions;

/// <summary>
/// "电源操作"行动设置
/// </summary>
public partial class PowerOptionsActionSettings : ObservableRecipient
{
    [ObservableProperty] 
    PowerOptionsType _powerOptionsType;

    int _delaySeconds=60;

    /// <summary>
    /// 延迟执行（秒）
    /// </summary>
    public int DelaySeconds
    {
        get => _delaySeconds;
        set
        {
            if(value == _delaySeconds) return;
            _delaySeconds = value;
            OnPropertyChanged();
        }
    }
}
/// <summary>
/// "电源操作"行动运行类型。
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PowerOptionsType
{
    /// <summary>
    /// 关机
    /// </summary>
    Shutdown,
    /// <summary>
    /// 重启
    /// </summary>
    Reboot,
    /// <summary>
    /// 休眠
    /// </summary>
    Hibernate,
    /// <summary>
    /// 睡眠
    /// </summary>
    Sleep
}