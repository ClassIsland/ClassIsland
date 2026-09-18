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
using ClassIsland.Core.Models.Theming;
using ClassIsland.Core.Models.UI;
using ClassIsland.Models;
using ClassIsland.Models.Profile;
using ClassIsland.Helpers;
using ClassIsland.Services;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Views;
using Avalonia.Media;
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
    public IThemeService ThemeService { get; }

    public SyncDictionaryList<Guid, ClassPlan> ClassPlans { get; }
    public SyncDictionaryList<Guid, TimeLayout> TimeLayouts { get; }
    public SyncDictionaryList<Guid, Subject> Subjects { get; }

    private ObservableCollection<SubjectSelectionItem> _subjectSelectionItems = [];
    public ObservableCollection<SubjectSelectionItem> SubjectSelectionItems
    {
        get => _subjectSelectionItems;
        private set => SetProperty(ref _subjectSelectionItems, value);
    }
    private ObservableCollection<SubjectGroupSelectionItem> _subjectGroupSelectionItems = [];
    public ObservableCollection<SubjectGroupSelectionItem> SubjectGroupSelectionItems
    {
        get => _subjectGroupSelectionItems;
        private set => SetProperty(ref _subjectGroupSelectionItems, value);
    }

    private ObservableCollection<SubjectGroupSelectionItem> _subjectGroupFilterItems = [];
    public ObservableCollection<SubjectGroupSelectionItem> SubjectGroupFilterItems
    {
        get => _subjectGroupFilterItems;
        private set => SetProperty(ref _subjectGroupFilterItems, value);
    }
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
    [ObservableProperty] private SubjectGroup? _selectedSubjectGroup;
    [ObservableProperty] private Color _selectedSubjectGroupColor = AccentColorPicker.GetCurrentAccentColor();
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
    private bool _isLoadingSelectedSubjectGroupColor;
    
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
        ITutorialService tutorialService,
        IThemeService themeService)
    {
        ProfileService = profileService;
        ManagementService = managementService;
        SettingsService = settingsService;
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;
        ActionService = actionService;
        Logger = logger;
        TutorialService = tutorialService;
        ThemeService = themeService;
        ThemeService.ThemeUpdated += ThemeServiceOnThemeUpdated;

        ClassPlans = new SyncDictionaryList<Guid, ClassPlan>(ProfileService.Profile.ClassPlans, Guid.NewGuid);
        TimeLayouts = new SyncDictionaryList<Guid, TimeLayout>(ProfileService.Profile.TimeLayouts, Guid.NewGuid);
        Subjects = new SyncDictionaryList<Guid, Subject>(ProfileService.Profile.Subjects, Guid.NewGuid);
        ClassPlanGroups =
            new SyncDictionaryList<Guid, ClassPlanGroup>(ProfileService.Profile.ClassPlanGroups, Guid.NewGuid);
        OrderedSchedules =
            new SyncDictionaryList<DateTime, OrderedSchedule>(ProfileService.Profile.OrderedSchedules, () => DateTime.MinValue);

        RefreshSubjectSelectionItems();
        RefreshSubjectGroupSelectionItems();
        EnsureProfileEventSubscriptions();
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

    private Profile? _subscribedProfile;
    private ObservableDictionary<Guid, Subject>? _subscribedSubjects;
    private ObservableDictionary<Guid, SubjectGroup>? _subscribedSubjectGroups;

    /// <summary>
    /// 订阅科目与分组变化，重复调用不会累积订阅。当前档案在启动时加载；
    /// 若调用方替换了档案或字典，再次调用可将这些订阅迁移到新实例。
    /// </summary>
    internal void EnsureProfileEventSubscriptions()
    {
        var profile = ProfileService.Profile;
        if (ReferenceEquals(_subscribedProfile, profile) &&
            ReferenceEquals(_subscribedSubjects, profile.Subjects) &&
            ReferenceEquals(_subscribedSubjectGroups, profile.SubjectGroups))
        {
            return;
        }

        if (_subscribedSubjects != null)
        {
            _subscribedSubjects.CollectionChanged -= SubjectsOnCollectionChanged;
            foreach (var subject in _subscribedSubjects.Values)
            {
                subject.PropertyChanged -= SubjectOnPropertyChanged;
            }
        }

        if (_subscribedSubjectGroups != null)
        {
            _subscribedSubjectGroups.CollectionChanged -= SubjectGroupsOnCollectionChanged;
            foreach (var group in _subscribedSubjectGroups.Values)
            {
                group.PropertyChanged -= SubjectGroupOnPropertyChanged;
            }
        }

        _subscribedProfile = profile;
        _subscribedSubjects = profile.Subjects;
        _subscribedSubjectGroups = profile.SubjectGroups;
        _subscribedSubjects.CollectionChanged += SubjectsOnCollectionChanged;
        _subscribedSubjectGroups.CollectionChanged += SubjectGroupsOnCollectionChanged;
        foreach (var subject in _subscribedSubjects.Values)
        {
            subject.PropertyChanged += SubjectOnPropertyChanged;
        }
        foreach (var group in _subscribedSubjectGroups.Values)
        {
            group.PropertyChanged += SubjectGroupOnPropertyChanged;
        }

        ScheduleSubjectViewsRefresh(refreshSubjectGroupSelectionItems: true);
    }

    private void SubjectOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Subject.GroupId))
        {
            return;
        }

        // 筛选在某个分组（含未分组）时，若被改动的正好是当前编辑的科目，就跟着它切换筛选：
        // 否则该行会立刻从列表里消失、右侧编辑面板也被清空，编辑中途被打断。
        if (sender is Subject subject && ReferenceEquals(subject, SelectedSubject) &&
            SelectedSubjectGroupId != AllSubjectGroupId &&
            subject.GroupId != SelectedSubjectGroupId &&
            (subject.GroupId == Guid.Empty || ProfileService.Profile.SubjectGroups.ContainsKey(subject.GroupId)))
        {
            SelectedSubjectGroupId = subject.GroupId;
        }

        ScheduleSubjectViewsRefresh();
    }

    private void SubjectGroupOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SubjectGroup.Name) or nameof(SubjectGroup.Color))
        {
            ScheduleSubjectViewsRefresh(e.PropertyName == nameof(SubjectGroup.Name));
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
        // 预设会先重建分组、再写入科目 GroupId；这里必须同步补齐下拉选项，
        // 否则 ComboBox 会把暂时无法匹配的 SelectedValue 清空且不再自动恢复。
        RefreshSubjectGroupSelectionItems();
        ScheduleSubjectViewsRefresh();
    }

    // 不订阅 Profile.EditingSubjects：对它的新增/删除都会写回 Subjects 并触发
    // Subjects.CollectionChanged，而 EditingSubjects 实例在 Subjects 被替换时会被整体重建，
    // 订阅它只会在档案重载后静默失效。

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
            if (_subjectGroupSelectionRefreshPending)
            {
                _subjectGroupSelectionRefreshPending = false;
                RefreshSubjectGroupSelectionItems();
            }
            RefreshSubjectSelectionItems();
            RefreshFilteredSubjects();
        }, DispatcherPriority.Background);
    }

    private void RefreshSubjectSelectionItems()
    {
        SubjectSelectionItems = SubjectSelectionHelper.CreateItems(SubjectSelectionItems, ProfileService.Profile);
    }

    partial void OnSelectedSubjectGroupIdChanged(Guid value)
    {
        SelectedSubjectGroup = ProfileService.Profile.SubjectGroups.TryGetValue(value, out var group)
            ? group
            : null;
        _isLoadingSelectedSubjectGroupColor = true;
        SelectedSubjectGroupColor = ParseSubjectGroupColor(group?.Color);
        _isLoadingSelectedSubjectGroupColor = false;
        OnPropertyChanged(nameof(CanEditSelectedSubjectGroup));
        ScheduleSubjectViewsRefresh();
    }

    partial void OnSelectedSubjectGroupColorChanged(Color value)
    {
        if (_isLoadingSelectedSubjectGroupColor || SelectedSubjectGroup == null)
        {
            return;
        }

        // 默认强调色保持为空，课表选课继续用主题文本强调色，并跟随系统强调色变化。
        SelectedSubjectGroup.Color = AccentColorPicker.IsCurrentAccentColor(value)
            ? string.Empty
            : value.ToString();
    }

    private void ThemeServiceOnThemeUpdated(object? sender, ThemeUpdatedEventArgs e)
    {
        // 延后一个调度周期：既保证 FluentAvalonia 已完成主题字典更新后再解析颜色，
        // 也规避系统外观变化事件可能来自非 UI 线程的情况。
        Dispatcher.UIThread.Post(RefreshSelectedSubjectGroupAccentColor);
    }

    /// <summary>
    /// 外观变化后重算「分组标题颜色」色块。色块展示的是当前外观下强调色的渲染结果，
    /// 因此主题或系统强调色变化后需要重算。仅在分组使用默认颜色（跟随系统强调色）时刷新，
    /// 避免覆盖用户自定义的颜色。
    /// </summary>
    internal void RefreshSelectedSubjectGroupAccentColor()
    {
        if (!string.IsNullOrWhiteSpace(SelectedSubjectGroup?.Color))
        {
            return;
        }

        _isLoadingSelectedSubjectGroupColor = true;
        SelectedSubjectGroupColor = AccentColorPicker.GetCurrentAccentColor();
        _isLoadingSelectedSubjectGroupColor = false;
    }

    private static Color ParseSubjectGroupColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AccentColorPicker.GetCurrentAccentColor();
        }

        try
        {
            return Color.Parse(value);
        }
        catch (FormatException)
        {
            return AccentColorPicker.GetCurrentAccentColor();
        }
    }

    internal void RefreshSubjectGroupSelectionItems()
    {
        var groups = ProfileService.Profile.SubjectGroups
            .Select(x => new SubjectGroupSelectionItem(x.Key, x.Value.Name))
            .ToList();

        SubjectGroupSelectionItems = SubjectSelectionHelper.CreateGroupItems(SubjectGroupSelectionItems,
        [
            new SubjectGroupSelectionItem(Guid.Empty, "未分组"),
            .. groups
        ]);
        SubjectGroupFilterItems = SubjectSelectionHelper.CreateGroupItems(SubjectGroupFilterItems,
        [
            new SubjectGroupSelectionItem(AllSubjectGroupId, "全部"),
            new SubjectGroupSelectionItem(Guid.Empty, "未分组"),
            .. groups
        ]);
    }

    internal void RefreshFilteredSubjects()
    {
        var subjects = ProfileService.Profile.EditingSubjects
            .Where(x => SelectedSubjectGroupId == AllSubjectGroupId ||
                        (SelectedSubjectGroupId == Guid.Empty
                            ? x.GroupId == Guid.Empty || !ProfileService.Profile.SubjectGroups.ContainsKey(x.GroupId)
                            : x.GroupId == SelectedSubjectGroupId))
            .ToList();

        SubjectSelectionHelper.Synchronize(FilteredSubjects, subjects);
        if (SelectedSubject != null && !subjects.Contains(SelectedSubject))
        {
            SelectedSubject = null;
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
