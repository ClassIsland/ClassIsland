using System;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Core.Abstractions.Services.Management;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using ClassIsland.Models.Profile;
using ClassIsland.Helpers;
using ClassIsland.Services;
using ClassIsland.Shared.Models.Profile;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;

namespace ClassIsland.ViewModels;

public partial class ClassChangingViewModel : ObservableRecipient
{
    [ObservableProperty] private bool _writeToSourceClassPlan = false;
    [ObservableProperty] private int _slideIndex = 0;
    [ObservableProperty] private int _sourceIndex = -1;
    [ObservableProperty] private int _swapModeTargetIndex = -1;
    [ObservableProperty] private Subject? _targetSubject;
    [ObservableProperty] private bool _isAutoNextStep = false;
    [ObservableProperty] private Guid _targetSubjectIndex;
    [ObservableProperty] private int _classChangeMode;
    [ObservableProperty] private bool _canCompleteClassChanging = false;
    [ObservableProperty] private ClassInfo? _selectedClassInfo;
    [ObservableProperty] private TimeLayoutItem? _selectedTimeLayoutItem;
    
    public SettingsService SettingsService { get; }

    public IProfileService ProfileService { get; }

    public IManagementService ManagementService { get; }

    private SyncDictionaryList<Guid, Subject>? _subjects;

    // 保留旧公开绑定入口；内置界面使用分组数据源，无外部调用时无需创建旧包装。
    public SyncDictionaryList<Guid, Subject> Subjects =>
        _subjects ??= new SyncDictionaryList<Guid, Subject>(ProfileService.Profile.Subjects, Guid.NewGuid);

    private ObservableCollection<SubjectSelectionItem> _subjectSelectionItems = [];
    public ObservableCollection<SubjectSelectionItem> SubjectSelectionItems
    {
        get => _subjectSelectionItems;
        private set => SetProperty(ref _subjectSelectionItems, value);
    }
    private bool _subjectSelectionRefreshPending;
    private bool _subjectSubscriptionsReleased;

    public ClassChangingViewModel(IProfileService profileService, IManagementService managementService, SettingsService settingsService)
    {
        ProfileService = profileService;
        ManagementService = managementService;
        SettingsService = settingsService;

        RefreshSubjectSelectionItems();
        ProfileService.Profile.Subjects.CollectionChanged += SubjectsOnCollectionChanged;
        ProfileService.Profile.SubjectGroups.CollectionChanged += SubjectGroupsOnCollectionChanged;
        foreach (var subject in ProfileService.Profile.Subjects.Values)
        {
            subject.PropertyChanged += SubjectOnPropertyChanged;
        }
        foreach (var group in ProfileService.Profile.SubjectGroups.Values)
        {
            group.PropertyChanged += SubjectGroupOnPropertyChanged;
        }
        this.ObservableForProperty(x => x.TargetSubjectIndex)
            .Subscribe(_ => UpdateCanCompleteClassChanging());
        this.ObservableForProperty(x => x.SwapModeTargetIndex)
            .Subscribe(_ => UpdateCanCompleteClassChanging());
        this.ObservableForProperty(x => x.SlideIndex)
            .Subscribe(_ => UpdateCanCompleteClassChanging());
        SettingsService.Settings.ObservableForProperty(x => x.IsSwapMode)
            .Subscribe(_ => UpdateCanCompleteClassChanging());
    }

    internal void ReleaseSubjectSubscriptions()
    {
        // 换课窗口每次打开都会创建新实例，不能让档案事件继续持有已关闭的窗口模型。
        _subjectSubscriptionsReleased = true;
        ProfileService.Profile.Subjects.CollectionChanged -= SubjectsOnCollectionChanged;
        ProfileService.Profile.SubjectGroups.CollectionChanged -= SubjectGroupsOnCollectionChanged;
        foreach (var subject in ProfileService.Profile.Subjects.Values)
        {
            subject.PropertyChanged -= SubjectOnPropertyChanged;
        }
        foreach (var group in ProfileService.Profile.SubjectGroups.Values)
        {
            group.PropertyChanged -= SubjectGroupOnPropertyChanged;
        }
    }

    private void SubjectsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in e.OldItems?.OfType<KeyValuePair<Guid, Subject>>() ?? [])
        {
            item.Value.PropertyChanged -= SubjectOnPropertyChanged;
        }
        foreach (var item in e.NewItems?.OfType<KeyValuePair<Guid, Subject>>() ?? [])
        {
            item.Value.PropertyChanged += SubjectOnPropertyChanged;
        }
        ScheduleSubjectSelectionItemsRefresh();
    }

    private void SubjectOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Subject.GroupId))
        {
            ScheduleSubjectSelectionItemsRefresh();
        }
    }

    private void SubjectGroupsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in e.OldItems?.OfType<KeyValuePair<Guid, SubjectGroup>>() ?? [])
        {
            item.Value.PropertyChanged -= SubjectGroupOnPropertyChanged;
        }
        foreach (var item in e.NewItems?.OfType<KeyValuePair<Guid, SubjectGroup>>() ?? [])
        {
            item.Value.PropertyChanged += SubjectGroupOnPropertyChanged;
        }
        ScheduleSubjectSelectionItemsRefresh();
    }

    private void SubjectGroupOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SubjectGroup.Name) or nameof(SubjectGroup.Color))
        {
            ScheduleSubjectSelectionItemsRefresh();
        }
    }

    private void ScheduleSubjectSelectionItemsRefresh()
    {
        if (_subjectSelectionRefreshPending)
        {
            return;
        }

        _subjectSelectionRefreshPending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _subjectSelectionRefreshPending = false;
            if (!_subjectSubscriptionsReleased)
            {
                RefreshSubjectSelectionItems();
            }
        }, DispatcherPriority.Background);
    }

    private void RefreshSubjectSelectionItems()
    {
        SubjectSelectionItems = SubjectSelectionHelper.CreateItems(SubjectSelectionItems, ProfileService.Profile);
    }

    private void UpdateCanCompleteClassChanging()
    {
        if (SlideIndex == 0)
        {
            CanCompleteClassChanging = false;
            return;
        }

        if (SettingsService.Settings.IsSwapMode)
        {
            CanCompleteClassChanging = SwapModeTargetIndex != -1;
            return;
        }

        CanCompleteClassChanging = TargetSubjectIndex != Guid.Empty;
    }
}
