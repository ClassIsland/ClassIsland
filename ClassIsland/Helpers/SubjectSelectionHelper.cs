using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ClassIsland.Models.Profile;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Helpers;

internal static class SubjectSelectionHelper
{
    public static ObservableCollection<SubjectSelectionItem> CreateItems(
        IEnumerable<SubjectSelectionItem>? previousItems, Profile profile) =>
        CreateItems(previousItems, profile.Subjects, profile.SubjectGroups);

    /// <summary>
    /// 按「分组 → 组内科目 → 未分组」的顺序生成选择项。科目与分组直接取自档案字典，
    /// 便于课表看板单元格等只拿到字典实例（而非整个 <see cref="Profile"/>）的调用方复用同一套排序。
    /// </summary>
    public static ObservableCollection<SubjectSelectionItem> CreateItems(
        IEnumerable<SubjectSelectionItem>? previousItems,
        IEnumerable<KeyValuePair<Guid, Subject>> subjects,
        IEnumerable<KeyValuePair<Guid, SubjectGroup>> subjectGroups)
    {
        var subjectList = subjects.ToList();
        var groupList = subjectGroups.ToList();
        var groupKeys = groupList.Select(x => x.Key).ToHashSet();
        var existingSubjects = previousItems?.Where(x => !x.IsGroupHeader).ToDictionary(x => x.Key)
                                ?? new Dictionary<Guid, SubjectSelectionItem>();
        var items = new List<SubjectSelectionItem>();
        SubjectSelectionItem GetSubjectItem(KeyValuePair<Guid, Subject> subject) =>
            existingSubjects.TryGetValue(subject.Key, out var item) && ReferenceEquals(item.Value, subject.Value)
                ? item
                : new SubjectSelectionItem(subject.Key, subject.Value);

        foreach (var group in groupList)
        {
            items.Add(new SubjectSelectionItem(Guid.Empty, null, group.Value.Name, group.Value.Color));
            items.AddRange(subjectList.Where(x => x.Value.GroupId == group.Key).Select(GetSubjectItem));
        }
        items.Add(new SubjectSelectionItem(Guid.Empty, null, "未分组", isUngroupedHeader: true));
        items.AddRange(subjectList
            .Where(x => x.Value.GroupId == Guid.Empty || !groupKeys.Contains(x.Value.GroupId))
            .Select(GetSubjectItem));
        // ComboBox 在 Move 当前项时也会丢失选择；整体换源并复用科目项可保留选择。
        return new ObservableCollection<SubjectSelectionItem>(items);
    }

    public static ObservableCollection<SubjectGroupSelectionItem> CreateGroupItems(
        IEnumerable<SubjectGroupSelectionItem> previousItems,
        IEnumerable<SubjectGroupSelectionItem> source)
    {
        var existingItems = previousItems.ToDictionary(x => x.Key);
        var items = source.Select(item =>
        {
            if (!existingItems.TryGetValue(item.Key, out var existing))
            {
                return item;
            }

            existing.Name = item.Name;
            return existing;
        });
        // 分组下拉框也不能 Move 已选项；换源时保留对象身份，兼顾改名与结构变化。
        return new ObservableCollection<SubjectGroupSelectionItem>(items);
    }

    public static void Synchronize<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        // Clear/Reset 会清空选择控件的选中项；保留现有对象并移动它们，以维持双向绑定的选择状态。
        for (var index = 0; index < source.Count; index++)
        {
            var existingIndex = target.IndexOf(source[index]);
            if (existingIndex < 0)
            {
                target.Insert(index, source[index]);
            }
            else if (existingIndex != index)
            {
                target.Move(existingIndex, index);
            }
        }
        while (target.Count > source.Count)
        {
            target.RemoveAt(target.Count - 1);
        }
    }
}
