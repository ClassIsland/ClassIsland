using System;
using System.Linq;
using ClassIsland.Shared.Models.Profile;
using Xunit;

namespace ClassIsland.Tests;

/// <summary>
/// 针对上游 #2006 的回归测试。
///
/// 问题：主界面课程表组件绑定的是 <see cref="ClassPlan.ValidTimeLayoutItems"/>，
/// 若该列表不按时间排序，显示顺序就会取决于数据在档案文件中的存储顺序——
/// 于是「先在末尾新增、再把时间改成正确值」的时间点会永远显示在末尾
/// （issue 原例：一节 14:40 的课被显示在 21:10 那节之后）。
///
/// <see cref="ClassPlan.ValidTimeLayoutItems"/> 依赖 <c>internal</c> 的 Profile 关联，
/// 因此这里直接测试它所依据的排序规则 <see cref="ClassPlan.OrderByTime"/>，
/// 并由 ProfileSettingsWindow 在时间变更后复用同一规则保持编辑器的顺序一致。
/// </summary>
public class ClassPlanTimeOrderingTests
{
    private static TimeLayoutItem OnClass(TimeSpan start, TimeSpan end)
        => new() { TimeType = 0, StartTime = start, EndTime = end };

    private static TimeLayoutItem Breaking(TimeSpan start, TimeSpan end)
        => new() { TimeType = 1, StartTime = start, EndTime = end };

    [Fact]
    public void OrderByTime_SortsByStartTime_RegardlessOfInputOrder()
    {
        var morning = OnClass(new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0));
        var noon = OnClass(new TimeSpan(14, 40, 0), new TimeSpan(14, 50, 0));
        var evening = OnClass(new TimeSpan(21, 10, 0), new TimeSpan(22, 0, 0));

        // 刻意错乱的输入顺序：早、晚、午 —— 复现 issue 里「先追加在末尾再改时间」的存储状态
        var ordered = ClassPlan.OrderByTime([morning, evening, noon]);

        Assert.Equal(
            new[] { morning.StartTime, noon.StartTime, evening.StartTime },
            ordered.Select(x => x.StartTime));
    }

    [Fact]
    public void OrderByTime_ThenByEndTime()
    {
        var longOne = OnClass(new TimeSpan(10, 0, 0), new TimeSpan(10, 50, 0));
        var shortOne = OnClass(new TimeSpan(10, 0, 0), new TimeSpan(10, 30, 0));

        var ordered = ClassPlan.OrderByTime([longOne, shortOne]);

        Assert.Equal(shortOne.EndTime, ordered[0].EndTime);
        Assert.Equal(longOne.EndTime, ordered[1].EndTime);
    }

    [Fact]
    public void OrderByTime_DoesNotAlterValues()
    {
        // 排序必须无损：秒值、TimeType 都不能被改动
        var a = Breaking(new TimeSpan(11, 0, 0), new TimeSpan(11, 29, 30));
        var b = OnClass(new TimeSpan(11, 29, 30), new TimeSpan(12, 0, 0));

        var ordered = ClassPlan.OrderByTime([a, b]);

        Assert.Equal(new TimeSpan(11, 29, 30), ordered[0].EndTime);
        Assert.Equal(new TimeSpan(11, 29, 30), ordered[1].StartTime);
        Assert.Equal(1, ordered[0].TimeType);
        Assert.Equal(0, ordered[1].TimeType);
    }

    [Fact]
    public void OrderByTime_PreservesSeconds()
    {
        // 与 #2007 相关：相邻课程严丝合缝（11:29:30 结尾接 11:29:30 开始），
        // 秒值若被抹掉会凭空裂出 30 秒空档。
        var first = OnClass(new TimeSpan(11, 0, 0), new TimeSpan(11, 29, 30));
        var second = Breaking(new TimeSpan(11, 29, 30), new TimeSpan(11, 40, 0));

        var ordered = ClassPlan.OrderByTime([first, second]);

        Assert.Equal(30, ordered[0].EndTime.Seconds);
        Assert.Equal(30, ordered[1].StartTime.Seconds);
    }

    [Fact]
    public void OrderByTime_IsStableForEmptyInput()
        => Assert.Empty(ClassPlan.OrderByTime([]));

    [Fact]
    public void OrderByTime_HandlesSingleItem()
    {
        var only = OnClass(new TimeSpan(9, 0, 0), new TimeSpan(9, 45, 0));

        var ordered = ClassPlan.OrderByTime([only]);

        Assert.Single(ordered);
        Assert.Same(only, ordered[0]);
    }
}
