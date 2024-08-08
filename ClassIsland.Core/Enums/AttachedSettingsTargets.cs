namespace ClassIsland.Core.Enums;

/// <summary>
/// 附加设置附加的类型
/// </summary>
[Flags]
public enum AttachedSettingsTargets
{
    /// <summary>
    /// 无
    /// </summary>
    None=0,
    /// <summary>
    /// 表示可以附加到课程类型上。
    /// </summary>
    Lesson=1,
    /// <summary>
    /// 表示可以附加到科目类型上。
    /// </summary>
    Subject=2,
    /// <summary>
    /// 表示可以附加到时间点类型上。
    /// </summary>
    TimePoint=4,
    /// <summary>
    /// 表示可以附加到课程表类型上。
    /// </summary>
    ClassPlan=8,
    /// <summary>
    /// 表示可以附加到时间表类型上。
    /// </summary>
    TimeLayout=16
}