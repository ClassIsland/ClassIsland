using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.IPC.Abstractions.Services;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 课程服务，用于存储当前课表状态与信息。
/// </summary>
public interface ILessonsService : INotifyPropertyChanged, INotifyPropertyChanging, IPublicLessonsService
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
    /// 启动主计时器。
    /// </summary>
    public void StartMainTimer();

    /// <summary>
    /// 停止主计时器。
    /// </summary>
    public void StopMainTimer();

    #endregion

    #region LessonsProperties

    internal TimeState CurrentOverlayStatus { get; set; }

    #endregion

    #region LessonsEvents

    /// <summary>
    /// 当上课时触发。
    /// </summary>
    event EventHandler? OnClass;

    /// <summary>
    /// 当课间休息时触发。
    /// </summary>
    event EventHandler? OnBreakingTime;

    /// <summary>
    /// 当放学时触发。
    /// </summary>
    event EventHandler? OnAfterSchool;

    /// <summary>
    /// 当当前时间状态改变时触发。
    /// </summary>
    event EventHandler? CurrentTimeStateChanged;

    internal void DebugTriggerOnClass();
    internal void DebugTriggerOnBreakingTime();
    internal void DebugTriggerOnAfterSchool();
    internal void DebugTriggerOnStateChanged();

    #endregion


    /// <summary>
    /// 根据日期获取当天的课表<see cref="ClassPlan"/>。如果那天没有课表安排，则返回 null
    /// </summary>
    /// <param name="date">要获取课表的日期</param>
    /// <param name="guid">获取到的课表的 GUID</param>
    /// <returns>获取到的课表</returns>
    ClassPlan? GetClassPlanByDate(DateTime date, out Guid? guid);

    /// <summary>
    /// 计算指定日期所在周在多周轮换（2周 ~ 最大周期）中的位置。
    /// </summary>
    /// <param name="referenceTime">指定日期。默认为当前日期。</param>
    /// <remarks> 2-first, 1-based </remarks>
    ObservableCollection<int> GetCyclePositionsByDate(DateTime? referenceTime = null);
}