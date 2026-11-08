using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Extensions;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.MainViewParts;

internal sealed class CourseEditViewModel : ObservableObject, IDisposable
{
    private readonly IProfileService _profileService;

    public CourseEditViewModel(IProfileService profileService, Guid itemId, DateOnly date)
    {
        _profileService = profileService;
        ItemId = itemId;
        Date = date;
        Item = profileService.Profile.ScheduleItems.GetValueOrDefault(itemId) ??
               throw new InvalidOperationException("找不到要编辑的课程。");
        Item.PropertyChanged += ItemOnPropertyChanged;
        profileService.Profile.Subjects.CollectionChanged += SubjectsOnCollectionChanged;
    }

    public ScheduleItem Item { get; }
    public Guid ItemId { get; }
    public DateOnly Date { get; }
    public Subject? Subject => _profileService.Profile.Subjects.GetValueOrDefault(Item.SubjectId);

    public string? ReplacementDescription
    {
        get
        {
            var target = Item.ReplacementTarget;
            if (target == null)
            {
                return null;
            }

            var date = DateOnly.FromDateTime(target.Date);
            if (!_profileService.Profile.ScheduleItems.TryGetValue(target.ScheduleItemId, out var targetItem))
            {
                return $"原课程已不存在（{date:yyyy年M月d日}）";
            }

            var subjectName = _profileService.Profile.Subjects.GetValueOrDefault(targetItem.SubjectId)?.Name;
            var displayName = string.IsNullOrWhiteSpace(subjectName) ? "未指定科目" : subjectName;
            return $"替换 {date:yyyy年M月d日} 的“{displayName}”课程";
        }
    }

    public string DurationText
    {
        get
        {
            var duration = Item.EndTime - Item.StartTime;
            return duration <= TimeSpan.Zero
                ? "结束时间必须晚于开始时间。"
                : $"课程持续 {duration.Hours} 小时 {duration.Minutes} 分钟。";
        }
    }

    public void ConvertToNormal()
    {
        if (Item.ReplacementTarget == null)
        {
            return;
        }

        var previous = Item.ReplacementTarget;
        if (!_profileService.ConvertScheduleItemReplacementToNormal(ItemId))
        {
            return;
        }
        _profileService.SaveProfile(() => Item.ReplacementTarget = previous);
    }

    public bool Commit()
    {
        if (Item.EndTime <= Item.StartTime)
        {
            return false;
        }

        _profileService.SaveProfile();
        return true;
    }

    public void Delete()
    {
        var items = _profileService.Profile.ScheduleItems;
        var item = new KeyValuePair<Guid, ScheduleItem>(ItemId, Item);
        var index = items.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        items.RemoveAt(index);
        _profileService.SaveProfile(() => items.Insert(index, item));
    }

    public void Dispose()
    {
        Item.PropertyChanged -= ItemOnPropertyChanged;
        _profileService.Profile.Subjects.CollectionChanged -= SubjectsOnCollectionChanged;
    }

    private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ScheduleItem.SubjectId):
                OnPropertyChanged(nameof(Subject));
                break;
            case nameof(ScheduleItem.StartTime):
            case nameof(ScheduleItem.EndTime):
                OnPropertyChanged(nameof(DurationText));
                break;
            case nameof(ScheduleItem.ReplacementTarget):
                OnPropertyChanged(nameof(ReplacementDescription));
                break;
        }
    }

    private void SubjectsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Subject));
        OnPropertyChanged(nameof(ReplacementDescription));
    }
}
