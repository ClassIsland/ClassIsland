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
    /// 主计时器是否正在工作
    /// </summary>
    bool IsTimerRunning { get; }

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

    /// <summary>
    /// 本周多周轮换周数。
    /// </summary>
    /// <remarks>
    /// 第 2 位 - 双周轮换<br/>
    /// 第 3 位 - 三周轮换<br/>
    /// ……<br/>
    /// <br/>
    /// 1 - 本周单周<br/>
    /// 2 - 本周是双周<br/>
    /// 3 - 本周是 3/x 周<br/>
    /// ……<br/>
    /// </remarks>
    ObservableCollection<int> MultiWeekRotation { get; set; }
}