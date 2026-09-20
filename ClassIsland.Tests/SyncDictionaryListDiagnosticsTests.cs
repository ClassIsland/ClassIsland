using System;
using System.Collections.Generic;
using System.Linq;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Profile;
using Xunit;

namespace ClassIsland.Tests;

/// <summary>
/// 调查上游 #2012「在档案编辑界面无法删除时间表」时，对 <see cref="SyncDictionaryList{TKey,TValue}"/>
/// 与 <see cref="ObservableDictionary{TKey,TValue}"/> 之间同步行为的诊断用例。
///
/// 目的：确认「字典里移除一项」是否能正确反映到绑定用的 List 上。
/// </summary>
public class SyncDictionaryListDiagnosticsTests
{
    private static (ObservableDictionary<Guid, TimeLayout> dict, SyncDictionaryList<Guid, TimeLayout> sync)
        Build()
    {
        var dict = new ObservableDictionary<Guid, TimeLayout>();
        var sync = new SyncDictionaryList<Guid, TimeLayout>(dict, Guid.NewGuid);
        return (dict, sync);
    }

    [Fact]
    public void Remove_FromDictionary_ShouldRemoveFromList()
    {
        var (dict, sync) = Build();
        var id = Guid.NewGuid();
        dict[id] = new TimeLayout { Name = "测试A" };

        Assert.Single(sync.List);

        dict.Remove(id);

        Assert.Empty(dict);
        Assert.Empty(sync.List);            // ← 期望同步；若失败即复现 #2012 的根因
    }

    [Fact]
    public void Remove_WithMultipleEntries_ShouldRemoveOnlyTarget()
    {
        var (dict, sync) = Build();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        dict[a] = new TimeLayout { Name = "A" };
        dict[b] = new TimeLayout { Name = "B" };

        Assert.Equal(2, sync.List.Count);

        dict.Remove(a);

        Assert.Single(sync.List);
        Assert.Equal(b, sync.List[0].Key);
    }

    [Fact]
    public void Remove_ViaFirstOrDefaultKeyPattern_ShouldWork()
    {
        // 复刻 ProfileSettingsWindow.ButtonDeleteTimeLayout_OnClick 的取 key 方式
        var (dict, sync) = Build();
        var id = Guid.NewGuid();
        var target = new TimeLayout { Name = "目标" };
        dict[id] = target;

        var selected = sync.List.First(x => x.Key == id).Value;   // 等价于 ViewModel.SelectedTimeLayout

        var key = dict.FirstOrDefault(x => x.Value == selected).Key;

        Assert.Equal(id, key);              // 若为 Guid.Empty，说明引用比对失败
        dict.Remove(key);
        Assert.Empty(sync.List);
    }

    [Fact]
    public void Remove_WhenSelectedInstanceIsNotInDictionary_ProducesEmptyGuid()
    {
        // 反向验证：如果 SelectedTimeLayout 与字典实例不同，取到的 key 会是 Guid.Empty，
        // Remove(Guid.Empty) 静默失败 —— 这正是 #2012 「点了没反应」的可能机制。
        var (dict, _) = Build();
        var id = Guid.NewGuid();
        dict[id] = new TimeLayout { Name = "真实" };

        var detached = new TimeLayout { Name = "游离实例" };
        var key = dict.FirstOrDefault(x => x.Value == detached).Key;

        Assert.Equal(Guid.Empty, key);
        Assert.False(dict.Remove(key));      // 静默失败，无异常、无提示
        Assert.Single(dict);
    }

    [Fact]
    public void Clear_Dictionary_ShouldClearList()
    {
        var (dict, sync) = Build();
        dict[Guid.NewGuid()] = new TimeLayout { Name = "X" };
        dict[Guid.NewGuid()] = new TimeLayout { Name = "Y" };
        Assert.Equal(2, sync.List.Count);

        dict.Clear();

        Assert.Empty(sync.List);            // Clear 走 Reset(?) 分支，可能不同步
    }
}
