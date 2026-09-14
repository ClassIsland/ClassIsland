using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Core.Models.Profile;
using ClassIsland.Core.Models.UI;
using ClassIsland.Models;
using ClassIsland.Models.Profile;
using ClassIsland.Services;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Views;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;


namespace ClassIsland.ViewModels;

public partial class ProfileSettingsViewModel : ObservableRecipient
{
    // “全部”只用于界面筛选，不能作为科目的实际分组写入档案。
    public static Guid AllSubjectGroupId { get; } = new("ffffffff-ffff-ffff-ffff-ffffffffffff");

    public IProfileService ProfileService { get; }
    public IManagementService ManagementService { get; }
    public SettingsService SettingsService { get; }
    public ILessonsService LessonsService { get; }
    public IExactTimeService ExactTimeService { get; }
    public IActionService ActionService { get; }
    public ILogger<ProfileSettingsWindow> Logger { get; }
    public ITutorialService TutorialService { get; }

    public SyncDictionaryList<Guid, ClassPlan> ClassPlans { get; }
    public SyncDictionaryList<Guid, TimeLayout> TimeLayouts { get; }
    public SyncDictionaryList<Guid, Subject> Subjects { get; }
    public SyncDictionaryList<Guid, SubjectGroup> SubjectGroups { get; }

    public ObservableCollection<SubjectSelectionItem> SubjectSelectionItems { get; } = [];
    public ObservableCollection<SubjectGroupSelectionItem> SubjectGroupSelectionItems { get; } = [];
    public ObservableCollection<SubjectGroupSelectionItem> SubjectGroupFilterItems { get; } = [];
    public ObservableCollection<Subject> FilteredSubjects { get; } = [];

    public bool CanEditSelectedSubjectGroup =>
        SelectedSubjectGroupId != AllSubjectGroupId && SelectedSubjectGroupId != Guid.Empty;

    public SyncDictionaryList<Guid, ClassPlanGroup> ClassPlanGroups { get; }
    public SyncDictionaryList<DateTime, OrderedSchedule> OrderedSchedules { get; }

    public IObservableList<KeyValuePair<Guid, ClassPlan>> TempClassPlanList { get; }


    [ObservableProperty] private ObservableCollection<object> _transferNavigationViewItems = [];
    [ObservableProperty] private object _drawerContent = new();
    [ObservableProperty] private bool _isClassPlansEditing = false;
    [ObservableProperty] private ObservableCollection<string> _profiles = new();
    [ObservableProperty] private bool _isRestartSnackbarActive = false;
    [ObservableProperty] private string _renameProfileName = "";
    [ObservableProperty] private string _createProfileName = "";
    [ObservableProperty] private string _selectedProfile = "";
    [ObservableProperty] private string _deleteConfirmField = "";
    [ObservableProperty] private bool _isOfflineEditor = false;
    [ObservableProperty] private TimeLayoutItem? _selectedTimePoint;
    [ObservableProperty] private double _timeLineScale = 3.0;
    [ObservableProperty] private Subject? _selectedSubject;
    [ObservableProperty] private Guid _selectedSubjectGroupId = AllSubjectGroupId;
    [ObservableProperty] private bool _isSubjectSelectionGroupRowMode;
    [ObservableProperty] private bool _isPanningModeEnabled = false;
    [ObservableProperty] private bool _isDragEntering = false;
    [ObservableProperty] private Guid _tempOverlayClassPlanTimeLayoutId = Guid.Empty;
    [ObservableProperty] private ClassInfo? _selectedClassInfo;
    [ObservableProperty] private int _selectedClassIndex = -1;
    [ObservableProperty] private ClassPlan? _selectedClassPlan = null;
    [ObservableProperty] private bool _isUpdatingClassInfoIndexInBackend = false;
    [ObservableProperty] private bool _isClassPlanEditComplete = false;
    [ObservableProperty] private bool _isWeekOffsetSettingsOpen = false;
    [ObservableProperty] private TimeLayoutItem? _previousTrackedTimeLayoutItem;
    [ObservableProperty] private DateTime _scheduleCalendarSelectedDate = DateTime.Today;
    [ObservableProperty] private DateTime _overlayEnableDateTime = DateTime.Today;
    [ObservableProperty] private bool _tempOverlayCreateTimeLayout = false;
    [ObservableProperty] private bool _isProfileImportMenuOpened = false;
    [ObservableProperty] private bool _isInScheduleSwappingMode = false;
    [ObservableProperty] private ScheduleClassPosition _classSwapEndPosition = ScheduleClassPosition.Zero;
    [ObservableProperty] private ScheduleClassPosition _classSwapStartPosition = ScheduleClassPosition.Zero;
    [ObservableProperty] private bool _isTempSwapMode = true;
    [ObservableProperty] private int _dataGridWeekRowsWeekIndex = 0;
    [ObservableProperty] private bool _isClassPlanTempEditPopupOpen = false;
    [ObservableProperty] private Guid _targetSubjectIndex = Guid.Empty;
    [ObservableProperty] private bool _isTimeLineSticky = true;
    [ObservableProperty] private bool _isDrawerOpen = false;
    [ObservableProperty] private int _masterPageTabSelectIndex = 0;
    [ObservableProperty] private TimeLayout? _selectedTimeLayout;
    [ObservableProperty] private int _selectedTimePointIndex = -1;
    [ObservableProperty] private bool _canUndo = false;
    [ObservableProperty] private bool _canRedo = false;
    public ObservableCollection<string> UndoDescriptions { get; } = [];
    public ObservableCollection<string> RedoDescriptions { get; } = [];
    [ObservableProperty] private ToastMessage? _currentTimePointDeleteRevertToast;
    [ObservableProperty] private ToastMessage? _currentClassPlanEditDoneToast = null;
    [ObservableProperty] private KeyValuePair<Guid, TimeLayout>? _classPlanInfoSelectedTimeLayoutKvp;
    [ObservableProperty] private HashSet<string> _currentProfileBreakNames = [];
    [ObservableProperty] private ProfileTransferProviderControlBase? _transferProviderContent;
    [ObservableProperty] private bool _isProfileTransferInvoked;
    [ObservableProperty] private ProfileTransferProviderInfo? _selectedTransferInfo;
    [ObservableProperty] private bool _isTransferring;
    [ObservableProperty] private int _selectedClassIndex2 = -1;
    
    [ObservableProperty] private ReadOnlyObservableCollection<ClassPlansTreeNode> _groupedClassPlans;
    private ClassPlansTreeNode? _selectedClassPlansTreeNode = null;
    private Guid _prevSelectedClassPlanGuid = Guid.Empty;
    private bool _subjectViewsRefreshPending;
    private bool _subjectGroupSelectionRefreshPending;
    
    public ClassPlansTreeNode? SelectedClassPlansTreeNode
    {
        get => _selectedClassPlansTreeNode;
        set
        {
            if (value == _selectedClassPlansTreeNode) return;

            _prevSelectedClassPlanGuid = _selectedClassPlansTreeNode?.Guid ?? Guid.Empty;
            _selectedClassPlansTreeNode = value;
            SelectedClassPlan = value?.ClassPlan;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc/>
    public ProfileSettingsViewModel(IProfileService profileService, IManagementService managementService,
        SettingsService settingsService, ILessonsService lessonsService, IExactTimeService exactTimeService,
        IActionService actionService,
        ILogger<ProfileSettingsWindow> logger,
        ITutorialService tutorialService)
    {
        ProfileService = profileService;
        ManagementService = managementService;
        SettingsService = settingsService;
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;
        ActionService = actionService;
        Logger = logger;
        TutorialService = tutorialService;

        ClassPlans = new SyncDictionaryList<Guid, ClassPlan>(ProfileService.Profile.ClassPlans, Guid.NewGuid);
        TimeLayouts = new SyncDictionaryList<Guid, TimeLayout>(ProfileService.Profile.TimeLayouts, Guid.NewGuid);
        Subjects = new SyncDictionaryList<Guid, Subject>(ProfileService.Profile.Subjects, Guid.NewGuid);
        SubjectGroups = new SyncDictionaryList<Guid, SubjectGroup>(ProfileService.Profile.SubjectGroups, Guid.NewGuid);
        ClassPlanGroups =
            new SyncDictionaryList<Guid, ClassPlanGroup>(ProfileService.Profile.ClassPlanGroups, Guid.NewGuid);
        OrderedSchedules =
            new SyncDictionaryList<DateTime, OrderedSchedule>(ProfileService.Profile.OrderedSchedules, () => DateTime.MinValue);

        RefreshSubjectSelectionItems();
        RefreshSubjectGroupSelectionItems();
        ProfileService.Profile.Subjects.CollectionChanged += SubjectsOnCollectionChanged;
        ProfileService.Profile.SubjectGroups.CollectionChanged += SubjectGroupsOnCollectionChanged;
        ProfileService.Profile.EditingSubjects.CollectionChanged += EditingSubjectsOnCollectionChanged;
        foreach (var subject in ProfileService.Profile.Subjects.Values)
        {
            subject.PropertyChanged += SubjectOnPropertyChanged;
        }
        foreach (var group in ProfileService.Profile.SubjectGroups.Values)
        {
            group.PropertyChanged += SubjectGroupOnPropertyChanged;
        }
        RefreshFilteredSubjects();

        TempClassPlanList = ClassPlans.List
            .ToObservableChangeSet()
            .Filter(x => !x.Value.IsOverlay)
            .AsObservableList();

        var classPlansSourceList = new SourceList<KeyValuePair<Guid, ClassPlan>>();
        foreach (var kvp in ProfileService.Profile.ClassPlans)
        {
            classPlansSourceList.Add(kvp);
        }

        ProfileService.Profile.ClassPlans
            .ToObservableChangeSet<ObservableDictionary<Guid, ClassPlan>, KeyValuePair<Guid, ClassPlan>>()
            .Subscribe(changeSet =>
            {
                foreach (var change in changeSet)
                {
                    switch (change.Reason)
                    {
                        case ListChangeReason.Add:
                            classPlansSourceList.Add(change.Item.Current);
                            break;
                        case ListChangeReason.Remove:
                            classPlansSourceList.Remove(change.Item.Current);
                            break;
                        case ListChangeReason.Replace:
                            classPlansSourceList.Replace(change.Item.Previous.Value, change.Item.Current);
                            break;
                    }
                }
            });
        
        classPlansSourceList.Connect()
            .Transform(pair => new ObservableKeyValuePair<Guid, ClassPlan>(pair))
            .AutoRefresh(pair => pair.Value.AssociatedGroup)
            .GroupOn(pair => pair.Value.AssociatedGroup)
            .Transform(group =>
            {
                group.List
                    .Connect()
                    .Transform(kv =>
                    {
                        var node = new ClassPlansTreeNode()
                        {
                            Guid = kv.Key,
                            IsGroup = false,
                            ClassPlan = kv.Value
                        };

                        if (kv.Key == _prevSelectedClassPlanGuid)
                        {
                            SelectedClassPlansTreeNode = node;
                        }
                        
                        return node;
                    })
                    .Bind(out var children)
                    .Subscribe();

                return new ClassPlansTreeNode()
                {
                    Guid = group.GroupKey,
                    IsGroup = true,
                    ClassPlan = null,
                    SubPlans = children
                };
            })
            .Bind(out _groupedClassPlans)
            .DisposeMany()
            .Subscribe();

        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(SelectedClassPlan))
            {
                SelectClassPlanByInstance(SelectedClassPlan, true);
            }
        };
    }

    private void SubjectOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Subject.GroupId))
        {
            ScheduleSubjectViewsRefresh();
        }
    }

    private void SubjectGroupOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SubjectGroup.Name))
        {
            ScheduleSubjectViewsRefresh(true);
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
        ScheduleSubjectViewsRefresh();
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
        ScheduleSubjectViewsRefresh(true);
    }

    private void EditingSubjectsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScheduleSubjectViewsRefresh();
    }

    private void ScheduleSubjectViewsRefresh(bool refreshSubjectGroupSelectionItems = false)
    {
        _subjectGroupSelectionRefreshPending |= refreshSubjectGroupSelectionItems;
        if (_subjectViewsRefreshPending)
        {
            return;
        }

        _subjectViewsRefreshPending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _subjectViewsRefreshPending = false;
            RefreshSubjectSelectionItems();
            RefreshFilteredSubjects();
            if (_subjectGroupSelectionRefreshPending)
            {
                _subjectGroupSelectionRefreshPending = false;
                RefreshSubjectGroupSelectionItems();
            }
        }, DispatcherPriority.Background);
    }

    private void RefreshSubjectSelectionItems()
    {
        var groups = ProfileService.Profile.SubjectGroups;
        var subjects = ProfileService.Profile.Subjects;
        var items = new List<SubjectSelectionItem>();

        foreach (var group in groups)
        {
            items.Add(new SubjectSelectionItem(Guid.Empty, null, group.Value.Name));
            items.AddRange(subjects
                .Where(x => x.Value.GroupId == group.Key)
                .Select(x => new SubjectSelectionItem(x.Key, x.Value)));
        }

        items.Add(new SubjectSelectionItem(Guid.Empty, null, "未分组"));
        items.AddRange(subjects
            .Where(x => x.Value.GroupId == Guid.Empty || !groups.ContainsKey(x.Value.GroupId))
            .Select(x => new SubjectSelectionItem(x.Key, x.Value)));

        SubjectSelectionItems.Clear();
        foreach (var item in items)
        {
            SubjectSelectionItems.Add(item);
        }

    }

    partial void OnSelectedSubjectGroupIdChanged(Guid value)
    {
        OnPropertyChanged(nameof(CanEditSelectedSubjectGroup));
        ScheduleSubjectViewsRefresh();
    }

    private void RefreshSubjectGroupSelectionItems()
    {
        var groups = ProfileService.Profile.SubjectGroups
            .Select(x => new SubjectGroupSelectionItem(x.Key, x.Value.Name))
            .ToList();

        SynchronizeSubjectGroupItems(SubjectGroupSelectionItems,
        [
            new SubjectGroupSelectionItem(Guid.Empty, "未分组"),
            .. groups
        ]);
        SynchronizeSubjectGroupItems(SubjectGroupFilterItems,
        [
            new SubjectGroupSelectionItem(AllSubjectGroupId, "全部"),
            new SubjectGroupSelectionItem(Guid.Empty, "未分组"),
            .. groups
        ]);
    }

    private void RefreshFilteredSubjects()
    {
        var subjects = ProfileService.Profile.EditingSubjects
            .Where(x => SelectedSubjectGroupId == AllSubjectGroupId ||
                        (SelectedSubjectGroupId == Guid.Empty
                            ? x.GroupId == Guid.Empty || !ProfileService.Profile.SubjectGroups.ContainsKey(x.GroupId)
                            : x.GroupId == SelectedSubjectGroupId))
            .ToList();

        FilteredSubjects.Clear();
        foreach (var subject in subjects)
        {
            FilteredSubjects.Add(subject);
        }
    }

    private static void SynchronizeSubjectGroupItems(
        ObservableCollection<SubjectGroupSelectionItem> target,
        IReadOnlyList<SubjectGroupSelectionItem> source)
    {
        for (var targetIndex = 0; targetIndex < source.Count; targetIndex++)
        {
            var sourceItem = source[targetIndex];
            var existingIndex = -1;
            for (var index = targetIndex; index < target.Count; index++)
            {
                if (target[index].Key == sourceItem.Key)
                {
                    existingIndex = index;
                    break;
                }
            }

            if (existingIndex < 0)
            {
                target.Insert(targetIndex, sourceItem);
            }
            else if (existingIndex != targetIndex)
            {
                target.Move(existingIndex, targetIndex);
            }

            target[targetIndex].Name = sourceItem.Name;
        }

        while (target.Count > source.Count)
        {
            target.RemoveAt(target.Count - 1);
        }
    }

    /// <summary>
    /// 通过 Guid 来选中课表。
    /// </summary>
    /// <param name="guid">要选中的课表 Guid</param>
    /// <returns>布尔值，true 为找到，false 为未找到。</returns>
    public bool SelectClassPlanByGuid(Guid guid)
    {
        foreach (var group in GroupedClassPlans)
        {
            if (group.SubPlans is null) continue;
            
            foreach (var child in group.SubPlans)
            {
                if (child.Guid != guid) continue;

                SelectedClassPlan = child.ClassPlan;
                SelectedClassPlansTreeNode = child;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 通过 课表实例 来选中课表。
    /// </summary>
    /// <param name="classPlan">要选中的课表实例</param>
    /// <param name="isInternal">是否为内部修改</param>
    /// <returns>布尔值，true 为找到，false 为未找到。</returns>
    public bool SelectClassPlanByInstance(ClassPlan? classPlan, bool isInternal = false)
    {
        if (classPlan == null) return false;
        
        foreach (var group in GroupedClassPlans)
        {
            if (group.SubPlans is null) continue;
            
            foreach (var child in group.SubPlans)
            {
                if (child.ClassPlan != classPlan) continue;

                if (!isInternal) SelectedClassPlan = child.ClassPlan;
                SelectedClassPlansTreeNode = child;
                return true;
            }
        }

        return false;
    }
}
