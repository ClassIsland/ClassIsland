using System.Collections.ObjectModel;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using dotnetCampus.Ipc.CompilerServices.Attributes;

namespace ClassIsland.Shared.IPC.Abstractions.Services;

/// <summary>
/// 向其它进程公开的课程服务，用于存储当前课表状态与信息。
/// </summary>
[IpcPublic(IgnoresIpcException = true)]
public interface IPublicLessonsService
{
    /// <summary>
    /// 主计时器是否正在工作。
    /// </summary>
    bool IsTimerRunning { get; }

    /// <summary>
    /// 当前加载的课表。如果当前没有课表，则为 null。
    /// </summary>
    ClassPlan? CurrentClassPlan { get; set; }

    /// <summary>
    /// 当前所处时间点<see cref="TimeLayoutItem"/>的索引。如无，则为 -1。
    /// </summary>
    int CurrentSelectedIndex { get; set; }

    /// <summary>
    /// 当前或下一节课（下一个上课类型的时间点<see cref="TimeLayoutItem"/>）的科目。如无，则为 <see cref="Subject.Empty"/>。
    /// </summary>
    Subject NextClassSubject { get; set; }

    /// <summary>
    /// 当前或下一个课间休息类型的时间点。如无，则为 <see cref="TimeLayoutItem.Empty"/>。
    /// </summary>
    TimeLayoutItem NextBreakingTimeLayoutItem { get; set; }

    /// <summary>
    /// 当前或下一个上课类型的时间点。如无，则为 <see cref="TimeLayoutItem.Empty"/>。
    /// </summary>
    TimeLayoutItem NextClassTimeLayoutItem { get; set; }

    /// <summary>
    /// 距上课剩余时间。如果当前正在上课，或没有下一节课程，则为 <see cref="TimeSpan.Zero"/>。
    /// </summary>
    TimeSpan OnClassLeftTime { get; set; }

    /// <summary>
    /// 距下课剩余时间。如果当前不在上课，则为 <see cref="TimeSpan.Zero"/>。
    /// </summary>
    TimeSpan OnBreakingTimeLeftTime { get; set; }

    /// <summary>
    /// 当前时间点状态。
    /// </summary>
    TimeState CurrentState { get; set; }

    /// <summary>
    /// 当前所处的时间点。如果当前没有时间点，则为 <see cref="TimeLayoutItem.Empty"/>。
    /// </summary>
    TimeLayoutItem CurrentTimeLayoutItem { get; set; }

    /// <summary>
    /// 当前所处时间点<see cref="TimeLayoutItem"/>的科目。<br/><br/>
    /// 如果当前是课间休息，则其中 <see cref="Subject.Name"/>(科目名) 为课间名称。<br/>
    /// 如果当前课程未定义，则为 <see cref="Subject.Empty"/>。<br/>
    /// 如果当前没有时间点，或没有加载课表，则为 null。<br/>
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
    /// 本周多周轮换周数。
    /// </summary>
    /// <value>
    /// 第 x 位数字是 y（MultiWeekRotation[x]=y）— 本周是 x 周轮换中的第 y 周。<br/>
    /// <br/>
    /// 例：<br/>
    /// 第 2 位数字是 1（MultiWeekRotation[2]=1）— 本周是 2 周轮换中的第 1 周。<br/>
    /// 第 4 位数字是 3（MultiWeekRotation[4]=3）— 本周是 4 周轮换中的第 3 周。<br/>
    /// </value>
    ObservableCollection<int> MultiWeekRotation { get; set; }

    /// <summary>
    /// 根据日期获取当天的课表<see cref="ClassPlan"/>。如果那天没有课表安排，则返回 null
    /// </summary>
    /// <param name="date">要获取课表的日期</param>
    /// <returns>获取到的课表</returns>
    ClassPlan? GetClassPlanByDate(DateTime date);
}