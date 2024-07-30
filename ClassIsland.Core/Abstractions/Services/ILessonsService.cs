using System.ComponentModel;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 课程服务，用于存储当前课表状态与信息。
/// </summary>
public interface ILessonsService : INotifyPropertyChanged, INotifyPropertyChanging
{
    #region Timer

    /// <summary>
    /// 在主计时器开始处理课表信息前触发。
    /// </summary>
    public event EventHandler? PreMainTimerTicked;

    /// <summary>
    /// 在主计时器完成处理课表信息后触发。
    /// </summary>
    public event EventHandler? PostMainTimerTicked;
    
    /// <summary>
    /// 主计时器是否正在工作
    /// </summary>
    public bool IsTimerRunning { get; }

    /// <summary>
    /// 启动主计时器。
    /// </summary>
    public void StartMainTimer();

    /// <summary>
    /// 停止主计时器。
    /// </summary>
    public void StopMainTimer();

    #endregion

    #region LessonsProperties

    /// <summary>
    /// 当前加载的课表。如果当前没有课表，则为null。
    /// </summary>
    ClassPlan? CurrentClassPlan { get; set; }

    /// <summary>
    /// 当前所处时间点<see cref="TimeLayoutItem"/>的索引。
    /// </summary>
    int? CurrentSelectedIndex { get; set; }

    /// <summary>
    /// 下一节课（下一个上课类型的时间点<see cref="TimeLayoutItem"/>）的科目。
    /// </summary>
    Subject NextClassSubject { get; set; }

    ///// <summary>
    ///// 下一个时间点。
    ///// </summary>
    //TimeLayoutItem NextTimeLayoutItem { get; set; }

    /// <summary>
    /// 下一个课间休息类型的时间点。
    /// </summary>
    TimeLayoutItem NextBreakingTimeLayoutItem { get; set; }

    /// <summary>
    /// 下一个上课类型的时间点。
    /// </summary>
    TimeLayoutItem NextClassTimeLayoutItem { get; set; }

    /// <summary>
    /// 距离上课剩余时间。
    /// </summary>
    TimeSpan OnClassLeftTime { get; set; }

    /// <summary>
    /// 当前时间点状态
    /// </summary>
    TimeState CurrentState { get; set; }

    internal TimeState CurrentOverlayStatus { get; set; }

    /// <summary>
    /// 当前所处的时间点。
    /// </summary>
    TimeLayoutItem CurrentTimeLayoutItem { get; set; }

    /// <summary>
    /// 当前所处时间点<see cref="TimeLayoutItem"/>的科目。如果没有加载课表，则为null。
    /// </summary>
    Subject? CurrentSubject { get; set; }

    /// <summary>
    /// 是否启用课表。
    /// </summary>
    bool IsClassPlanEnabled { get; set; }

    /// <summary>
    /// 是否已加载课表。
    /// </summary>
    bool IsClassPlanLoaded { get; set; }

    /// <summary>
    /// 是否已确定当前时间点。
    /// </summary>
    bool IsLessonConfirmed { get; set; }

    /// <summary>
    /// 距下课剩余时间
    /// </summary>
    TimeSpan OnBreakingTimeLeftTime { get; set; }

    #endregion

    #region LessonsEvents

    /// <summary>
    /// 当上课时触发。
    /// </summary>
    public event EventHandler? OnClass;

    /// <summary>
    /// 当课间休息时触发。
    /// </summary>
    public event EventHandler? OnBreakingTime;

    /// <summary>
    /// 当放学时触发。
    /// </summary>
    public event EventHandler? OnAfterSchool;

    /// <summary>
    /// 当当前时间状态改变时触发。
    /// </summary>
    public event EventHandler? CurrentTimeStateChanged;

    internal void DebugTriggerOnClass();
    internal void DebugTriggerOnBreakingTime();
    internal void DebugTriggerOnAfterSchool();
    internal void DebugTriggerOnStateChanged();

    #endregion
}