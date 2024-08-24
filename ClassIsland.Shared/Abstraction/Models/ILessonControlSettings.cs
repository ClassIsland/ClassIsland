using System.ComponentModel;

namespace ClassIsland.Shared.Abstraction.Models;

/// <summary>
/// 课程显示设置接口
/// </summary>
public interface ILessonControlSettings : INotifyPropertyChanged
{
    /// <summary>
    /// 是否在当前时间点上显示附加信息
    /// </summary>
    public bool ShowExtraInfoOnTimePoint
    {
        get;
        set;
    }

    /// <summary>
    /// 时间点附加信息类型
    /// </summary>
    /// <value>
    /// 0
    /// </value>
    public int ExtraInfoType
    {
        get;
        set;
    }

    /// <summary>
    /// 是否启用时间点结束倒计时
    /// </summary>
    public bool IsCountdownEnabled
    {
        get;
        set;
    }

    /// <summary>
    /// 时间点结束倒计时时长
    /// </summary>
    public int CountdownSeconds
    {
        get;
        set;
    }
}