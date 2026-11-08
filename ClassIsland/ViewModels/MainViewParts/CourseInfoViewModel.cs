using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.MainViewParts;

internal sealed class CourseInfoViewModel : ObservableObject, IDisposable
{
    private readonly IProfileService _profileService;
    private readonly IManagementService _managementService;
    private readonly ClassInfo? _classInfo;
    private Subject? _subject;

    public CourseInfoViewModel(IProfileService profileService, IManagementService managementService, Guid scheduleItemId)
    {
        _profileService = profileService;
        _managementService = managementService;
        ResolvedScheduleItemId = scheduleItemId;
        Item = profileService.Profile.ScheduleItems.GetValueOrDefault(scheduleItemId);
        if (Item != null)
        {
            Item.PropertyChanged += ItemOnPropertyChanged;
        }
        AttachSubject();
        profileService.Profile.Subjects.CollectionChanged += SubjectsOnCollectionChanged;
    }

    public CourseInfoViewModel(IProfileService profileService, IManagementService managementService, ClassInfo classInfo)
    {
        _profileService = profileService;
        _managementService = managementService;
        _classInfo = classInfo;
        classInfo.PropertyChanged += ClassInfoOnPropertyChanged;
        AttachSubject();
        profileService.Profile.Subjects.CollectionChanged += SubjectsOnCollectionChanged;
    }

    public ScheduleItem? Item { get; private set; }
    public Guid ResolvedScheduleItemId { get; private set; }

    public Subject? Subject
    {
        get => _subject;
        private set => SetProperty(ref _subject, value);
    }

    public bool CanEdit => Item != null &&
                           _profileService.Profile.ScheduleType == ScheduleType.Schedule &&
                           _managementService.Policy.CanEditProfile;
    public string DisableButtonText => Item is { IsEnabled: false } ? "启用" : "禁用";

    public string TeacherText => string.IsNullOrWhiteSpace(Subject?.TeacherName)
        ? "未设置科任老师"
        : Subject.TeacherName;

    public string TimeText
    {
        get
        {
            TimeSpan start;
            TimeSpan end;
            if (Item != null)
            {
                start = Item.StartTime;
                end = Item.EndTime;
            }
            else if (_classInfo != null)
            {
                var time = _classInfo.CurrentTimeLayoutItem;
                start = time.StartTime;
                end = time.EndTime;
            }
            else
            {
                return "";
            }

            return $"{start:hh\\:mm}-{end:hh\\:mm}  {FormatDuration(end - start)}";
        }
    }

    public string LocationText
    {
        get
        {
            var location = Item != null
                ? string.IsNullOrWhiteSpace(Item.Location) ? Subject?.Location : Item.Location
                : _classInfo?.EffectiveLocation;
            return string.IsNullOrWhiteSpace(location) ? "未设置地点" : location;
        }
    }

    public void SwitchToScheduleItem(Guid scheduleItemId)
    {
        if (_classInfo != null || ResolvedScheduleItemId == scheduleItemId)
        {
            return;
        }

        if (Item != null)
        {
            Item.PropertyChanged -= ItemOnPropertyChanged;
        }

        ResolvedScheduleItemId = scheduleItemId;
        Item = _profileService.Profile.ScheduleItems.GetValueOrDefault(scheduleItemId);
        if (Item != null)
        {
            Item.PropertyChanged += ItemOnPropertyChanged;
        }

        AttachSubject();
        OnPropertyChanged(nameof(Item));
        NotifyDetailsChanged();
    }

    public void Dispose()
    {
        if (Item != null)
        {
            Item.PropertyChanged -= ItemOnPropertyChanged;
        }
        if (_classInfo != null)
        {
            _classInfo.PropertyChanged -= ClassInfoOnPropertyChanged;
        }
        if (Subject != null)
        {
            Subject.PropertyChanged -= SubjectOnPropertyChanged;
        }
        _profileService.Profile.Subjects.CollectionChanged -= SubjectsOnCollectionChanged;
    }

    private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ScheduleItem.SubjectId))
        {
            AttachSubject();
        }
        NotifyDetailsChanged();
    }

    private void ClassInfoOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ClassInfo.SubjectId) or nameof(ClassInfo.Subject))
        {
            AttachSubject();
        }
        NotifyDetailsChanged();
    }

    private void SubjectOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        NotifyDetailsChanged();
    }

    private void SubjectsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        AttachSubject();
        NotifyDetailsChanged();
    }

    private void AttachSubject()
    {
        if (Subject != null)
        {
            Subject.PropertyChanged -= SubjectOnPropertyChanged;
        }

        var subjectId = Item?.SubjectId ?? _classInfo?.SubjectId ?? Guid.Empty;
        Subject = _profileService.Profile.Subjects.GetValueOrDefault(subjectId);
        if (Subject != null)
        {
            Subject.PropertyChanged += SubjectOnPropertyChanged;
        }
    }

    private void NotifyDetailsChanged()
    {
        OnPropertyChanged(nameof(TeacherText));
        OnPropertyChanged(nameof(TimeText));
        OnPropertyChanged(nameof(LocationText));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(DisableButtonText));
    }

    private static string FormatDuration(TimeSpan duration) =>
        $"{(int)duration.TotalHours}小时{duration.Minutes}分钟";
}
