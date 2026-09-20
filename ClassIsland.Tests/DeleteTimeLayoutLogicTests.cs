using System;
using System.Collections.Generic;
using System.Linq;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Profile;
using Xunit;

namespace ClassIsland.Tests;

/// <summary>
/// 复刻 <c>ProfileSettingsWindow.ButtonDeleteTimeLayout_OnClick</c> 的完整判定逻辑，
/// 用于定位上游 #2012「点击删除时间表后没有删除」。
///
/// 该 handler 的流程是：
///   1. 以引用相等从 Profile.TimeLayouts 反查选中时间表的 key
///   2. 若存在 ClassPlans.TimeLayoutId == key，则提示「仍有课表在使用该时间表」并中止
///   3. 否则 Remove(key)
///
/// 本文件把「判定 → 移除」这段逻辑独立出来，用真实数据结构验证其行为。
/// </summary>
public class DeleteTimeLayoutLogicTests
{
    /// <summary>与 handler 中取 key 的方式一致。</summary>
    private static Guid ResolveKey(ObservableDictionary<Guid, TimeLayout> dict, TimeLayout? selected)
        => dict.FirstOrDefault(x => x.Value == selected).Key;

    private static bool IsStillInUse(IEnumerable<ClassPlan> plans, Guid key)
        => plans.Any(x => x.TimeLayoutId == key);

    [Fact]
    public void Delete_Succeeds_WhenNoClassPlanUsesTheLayout()
    {
        var dict = new ObservableDictionary<Guid, TimeLayout>();
        var sync = new SyncDictionaryList<Guid, TimeLayout>(dict, Guid.NewGuid);
        var key = Guid.NewGuid();
        dict[key] = new TimeLayout { Name = "空闲时间表" };
        var plans = new List<ClassPlan>();

        var selected = sync.List.Single().Value;
        var resolved = ResolveKey(dict, selected);

        Assert.Equal(key, resolved);
        Assert.False(IsStillInUse(plans, resolved));

        Assert.True(dict.Remove(resolved));
        Assert.Empty(sync.List);
    }

    [Fact]
    public void Delete_IsBlocked_WhenAClassPlanUsesTheLayout()
    {
        var dict = new ObservableDictionary<Guid, TimeLayout>();
        var key = Guid.NewGuid();
        dict[key] = new TimeLayout { Name = "在用时间表" };
        var plans = new List<ClassPlan> { new() { Name = "课表", TimeLayoutId = key } };

        var resolved = ResolveKey(dict, dict[key]);

        Assert.True(IsStillInUse(plans, resolved));   // 预期被拦
    }

    [Fact]
    public void Delete_IsSilentlyIgnored_WhenSelectedLayoutIsNotInDictionary()
    {
        // 若 SelectedTimeLayout 与字典内实例不是同一引用，key 会退化为 Guid.Empty，
        // Remove(Guid.Empty) 返回 false，界面毫无反应 —— 无异常、无提示。
        var dict = new ObservableDictionary<Guid, TimeLayout>();
        var sync = new SyncDictionaryList<Guid, TimeLayout>(dict, Guid.NewGuid);
        dict[Guid.NewGuid()] = new TimeLayout { Name = "真实" };
        var plans = new List<ClassPlan>();

        var detached = new TimeLayout { Name = "游离" };
        var resolved = ResolveKey(dict, detached);

        Assert.Equal(Guid.Empty, resolved);
        Assert.False(IsStillInUse(plans, resolved));
        Assert.False(dict.Remove(resolved));          // 静默失败
        Assert.Single(sync.List);                    // UI 上时间表仍在
    }

    [Fact]
    public void Delete_IsBlocked_WhenTargetKeyIsEmptyGuid_AndAPlanHasDefaultTimeLayoutId()
    {
        // ClassPlan.TimeLayoutId 默认值是 Guid.Empty。若目标时间表 key 恰好为 Guid.Empty，
        // 任何未设置时间表的课表都会让 IsStillInUse 返回 true，从而无法删除。
        var dict = new ObservableDictionary<Guid, TimeLayout>();
        var sync = new SyncDictionaryList<Guid, TimeLayout>(dict, Guid.NewGuid);
        dict[Guid.Empty] = new TimeLayout { Name = "空键时间表" };
        var plans = new List<ClassPlan> { new() { Name = "未设置时间表的课表" } };  // TimeLayoutId 默认 Guid.Empty

        var selected = sync.List.Single().Value;
        var resolved = ResolveKey(dict, selected);

        Assert.Equal(Guid.Empty, resolved);
        Assert.True(IsStillInUse(plans, resolved));   // ← 被误拦
    }
}
