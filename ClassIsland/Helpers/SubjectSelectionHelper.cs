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
        IEnumerable<SubjectSelectionItem> previousItems, Profile profile)
    {
        var existingSubjects = previousItems.Where(x => !x.IsGroupHeader).ToDictionary(x => x.Key);
        var items = new List<SubjectSelectionItem>();
        SubjectSelectionItem GetSubjectItem(KeyValuePair<Guid, Subject> subject) =>
            existingSubjects.TryGetValue(subject.Key, out var item) && ReferenceEquals(item.Value, subject.Value)
                ? item
                : new SubjectSelectionItem(subject.Key, subject.Value);

        foreach (var group in profile.SubjectGroups)
        {
            items.Add(new SubjectSelectionItem(Guid.Empty, null, group.Value.Name, group.Value.Color));
            items.AddRange(profile.Subjects.Where(x => x.Value.GroupId == group.Key).Select(GetSubjectItem));
        }
        items.Add(new SubjectSelectionItem(Guid.Empty, null, "未分组", isUngroupedHeader: true));
        items.AddRange(profile.Subjects
            .Where(x => x.Value.GroupId == Guid.Empty || !profile.SubjectGroups.ContainsKey(x.Value.GroupId))
            .Select(GetSubjectItem));
        // ComboBox 在 Move 当前项时也会丢失选择；整体换源并复用科目项可保留选择。
        return new ObservableCollection<SubjectSelectionItem>(items);
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
