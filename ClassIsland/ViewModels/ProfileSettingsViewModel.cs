using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;


namespace ClassIsland.ViewModels;

public partial class ProfileSettingsViewModel : ObservableRecipient
{
    private readonly List<IDisposable> _externalSubscriptions = [];
    private bool _resourcesReleased;

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

    public SyncDictionaryList<Guid, ClassPlanGroup> ClassPlanGroups { get; }
    public SyncDictionaryList<DateTime, OrderedSchedule> OrderedSchedules { get; }

    public IObservableList<KeyValuePair<Guid, ClassPlan>> TempClassPlanList { get; }


    [ObservableProperty] private ObservableCollection<object> _transferNavigationViewItems = [];
    [ObservableProperty] private object? _drawerContent = new();
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
    [ObservableProperty] private KeyValuePair<Guid, Subject>? _selectedSubjectKvp;
    [ObservableProperty] private Subject? _selectedSubject;
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
    [ObservableProperty] private KeyValuePair<Guid, ClassPlanGroup>? _classPlanInfoSelectedClassPlanGroupKvp;
    [ObservableProperty] private HashSet<string> _currentProfileBreakNames = [];
    [ObservableProperty] private ProfileTransferProviderControlBase? _transferProviderContent;
    [ObservableProperty] private bool _isProfileTransferInvoked;
    [ObservableProperty] private ProfileTransferProviderInfo? _selectedTransferInfo;
    [ObservableProperty] private bool _isTransferring;
    [ObservableProperty] private int _selectedClassIndex2 = -1;
    
    [ObservableProperty] private ReadOnlyObservableCollection<ClassPlansTreeNode> _groupedClassPlans;
    private readonly ObservableCollection<ClassPlansTreeNode> _groupedClassPlanNodes = [];
    private readonly Dictionary<Guid, ClassPlansTreeNode> _classPlanTreeGroupNodes = [];
    private readonly Dictionary<Guid, ObservableCollection<ClassPlansTreeNode>> _classPlanTreeGroupChildren = [];
    private readonly Dictionary<Guid, ClassPlansTreeNode> _classPlanTreeNodes = [];
    private ClassPlansTreeNode? _selectedClassPlansTreeNode = null;
    private bool _suppressClassPlanTreeSynchronization;
    
    public ClassPlansTreeNode? SelectedClassPlansTreeNode
    {
        get => _selectedClassPlansTreeNode;
        set
        {
            if (value == _selectedClassPlansTreeNode) return;

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
        ClassPlanGroups =
            new SyncDictionaryList<Guid, ClassPlanGroup>(ProfileService.Profile.ClassPlanGroups, Guid.NewGuid);
        OrderedSchedules =
            new SyncDictionaryList<DateTime, OrderedSchedule>(ProfileService.Profile.OrderedSchedules, () => DateTime.MinValue);

        TempClassPlanList = ClassPlans.List
            .ToObservableChangeSet()
            .Filter(x => !x.Value.IsOverlay)
            .AsObservableList();

        _groupedClassPlans = new ReadOnlyObservableCollection<ClassPlansTreeNode>(_groupedClassPlanNodes);
        SynchronizeClassPlanTree();

        _externalSubscriptions.Add(ClassPlanGroups.List
            .ToObservableChangeSet()
            .Subscribe(_ =>
            {
                if (!_suppressClassPlanTreeSynchronization)
                {
                    SynchronizeClassPlanTree();
                }
            }));

        _externalSubscriptions.Add(ClassPlans.List
            .ToObservableChangeSet()
            .Transform(pair => new ObservableKeyValuePair<Guid, ClassPlan>(pair))
            .DisposeMany()
            .AutoRefresh(pair => pair.Value.AssociatedGroup)
            .Subscribe(_ =>
            {
                if (!_suppressClassPlanTreeSynchronization)
                {
                    SynchronizeClassPlanTree();
                }
            }));

        PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SelectedClassPlan))
        {
            SelectClassPlanByInstance(SelectedClassPlan, true);
        }
    }

    partial void OnSelectedSubjectKvpChanged(KeyValuePair<Guid, Subject>? value)
    {
        SelectedSubject = value?.Value;
    }

    partial void OnSelectedSubjectChanged(Subject? value)
    {
        if (value == null)
        {
            SelectedSubjectKvp = null;
            return;
        }

        if (SelectedSubjectKvp is { } selected && ReferenceEquals(selected.Value, value))
        {
            return;
        }

        foreach (var subject in Subjects.List)
        {
            if (ReferenceEquals(subject.Value, value))
            {
                SelectedSubjectKvp = subject;
                return;
            }
        }

        SelectedSubjectKvp = null;
    }

    public void ReleaseResources()
    {
        if (_resourcesReleased)
        {
            return;
        }

        _resourcesReleased = true;
        PropertyChanged -= OnViewModelPropertyChanged;
        foreach (var subscription in _externalSubscriptions)
        {
            subscription.Dispose();
        }
        _externalSubscriptions.Clear();
        (TempClassPlanList as IDisposable)?.Dispose();

        ClassPlans.Dispose();
        TimeLayouts.Dispose();
        Subjects.Dispose();
        ClassPlanGroups.Dispose();
        OrderedSchedules.Dispose();

        CurrentTimePointDeleteRevertToast?.Close();
        CurrentClassPlanEditDoneToast?.Close();
        CurrentTimePointDeleteRevertToast = null;
        CurrentClassPlanEditDoneToast = null;
        DrawerContent = null;
        (TransferProviderContent as IDisposable)?.Dispose();
        TransferProviderContent = null;
        SelectedTransferInfo = null;
        SelectedTimePoint = null;
        SelectedTimeLayout = null;
        SelectedSubjectKvp = null;
        SelectedSubject = null;
        SelectedClassInfo = null;
        SelectedClassPlan = null;
        SelectedClassPlansTreeNode = null;
        ClassPlanInfoSelectedTimeLayoutKvp = null;
        ClassPlanInfoSelectedClassPlanGroupKvp = null;

        TransferNavigationViewItems.Clear();
        UndoDescriptions.Clear();
        RedoDescriptions.Clear();
        _groupedClassPlanNodes.Clear();
        _classPlanTreeGroupNodes.Clear();
        _classPlanTreeGroupChildren.Clear();
        _classPlanTreeNodes.Clear();
    }

    private ClassPlansTreeNode CreateClassPlanGroupNode(Guid groupId)
    {
        var children = new ObservableCollection<ClassPlansTreeNode>();
        var node = new ClassPlansTreeNode
        {
            Guid = groupId,
            IsGroup = true,
            SubPlans = new ReadOnlyObservableCollection<ClassPlansTreeNode>(children)
        };

        _classPlanTreeGroupChildren[groupId] = children;
        return node;
    }

    private void SynchronizeClassPlanTree()
    {
        var selectedGuid = SelectedClassPlansTreeNode is { IsGroup: false } selectedNode
            ? selectedNode.Guid
            : Guid.Empty;

        SynchronizeClassPlanTreeGroups();

        var classPlans = ClassPlans.List.ToList();
        var classPlanIds = classPlans.Select(pair => pair.Key).ToHashSet();
        foreach (var staleId in _classPlanTreeNodes.Keys.Where(id => !classPlanIds.Contains(id)).ToList())
        {
            _classPlanTreeNodes.Remove(staleId);
        }

        foreach (var pair in classPlans)
        {
            if (_classPlanTreeNodes.TryGetValue(pair.Key, out var existingNode)
                && ReferenceEquals(existingNode.ClassPlan, pair.Value))
            {
                continue;
            }

            _classPlanTreeNodes[pair.Key] = new ClassPlansTreeNode
            {
                Guid = pair.Key,
                IsGroup = false,
                ClassPlan = pair.Value
            };
        }

        var desiredChildren = _groupedClassPlanNodes.ToDictionary(
            group => group.Guid,
            _ => new List<ClassPlansTreeNode>());
        foreach (var pair in classPlans)
        {
            desiredChildren[pair.Value.AssociatedGroup].Add(_classPlanTreeNodes[pair.Key]);
        }

        // Remove nodes from their old groups before inserting into the new groups.
        foreach (var group in _groupedClassPlanNodes)
        {
            var children = _classPlanTreeGroupChildren[group.Guid];
            var desiredSet = desiredChildren[group.Guid].ToHashSet();
            for (var i = children.Count - 1; i >= 0; i--)
            {
                if (!desiredSet.Contains(children[i]))
                {
                    children.RemoveAt(i);
                }
            }
        }

        foreach (var group in _groupedClassPlanNodes)
        {
            var children = _classPlanTreeGroupChildren[group.Guid];
            var desired = desiredChildren[group.Guid];
            if (TrySynchronizeClassPlanTreeChildrenWithSingleMove(children, desired))
            {
                continue;
            }

            for (var targetIndex = 0; targetIndex < desired.Count; targetIndex++)
            {
                var child = desired[targetIndex];
                var currentIndex = children.IndexOf(child);
                if (currentIndex < 0)
                {
                    children.Insert(targetIndex, child);
                }
                else if (currentIndex != targetIndex)
                {
                    children.Move(currentIndex, targetIndex);
                }
            }
        }

        if (selectedGuid != Guid.Empty
            && _classPlanTreeNodes.TryGetValue(selectedGuid, out var restoredNode)
            && !ReferenceEquals(SelectedClassPlansTreeNode, restoredNode))
        {
            SelectedClassPlansTreeNode = restoredNode;
        }
    }

    private static bool TrySynchronizeClassPlanTreeChildrenWithSingleMove(
        ObservableCollection<ClassPlansTreeNode> children,
        List<ClassPlansTreeNode> desired)
    {
        if (children.Count != desired.Count)
        {
            return false;
        }

        if (children.SequenceEqual(desired))
        {
            return true;
        }

        for (var sourceIndex = 0; sourceIndex < children.Count; sourceIndex++)
        {
            var targetIndex = desired.IndexOf(children[sourceIndex]);
            if (targetIndex == sourceIndex
                || targetIndex < 0
                || !MatchesClassPlanTreeOrderAfterMove(children, desired, sourceIndex, targetIndex))
            {
                continue;
            }

            children.Move(sourceIndex, targetIndex);
            return true;
        }

        return false;
    }

    private static bool MatchesClassPlanTreeOrderAfterMove(
        ObservableCollection<ClassPlansTreeNode> children,
        List<ClassPlansTreeNode> desired,
        int sourceIndex,
        int targetIndex)
    {
        for (var index = 0; index < children.Count; index++)
        {
            var currentIndex = index;
            if (sourceIndex < targetIndex)
            {
                if (index >= sourceIndex && index < targetIndex)
                {
                    currentIndex = index + 1;
                }
                else if (index == targetIndex)
                {
                    currentIndex = sourceIndex;
                }
            }
            else
            {
                if (index == targetIndex)
                {
                    currentIndex = sourceIndex;
                }
                else if (index > targetIndex && index <= sourceIndex)
                {
                    currentIndex = index - 1;
                }
            }

            if (!ReferenceEquals(children[currentIndex], desired[index]))
            {
                return false;
            }
        }

        return true;
    }

    private void SynchronizeClassPlanTreeGroups()
    {
        var groupIds = ClassPlanGroups.List
            .Select(pair => pair.Key)
            .Distinct()
            .ToList();
        var knownGroupIds = groupIds.ToHashSet();

        foreach (var pair in ClassPlans.List)
        {
            if (knownGroupIds.Add(pair.Value.AssociatedGroup))
            {
                groupIds.Add(pair.Value.AssociatedGroup);
            }
        }

        var desiredGroupIds = groupIds.ToHashSet();
        for (var i = _groupedClassPlanNodes.Count - 1; i >= 0; i--)
        {
            var group = _groupedClassPlanNodes[i];
            if (desiredGroupIds.Contains(group.Guid))
            {
                continue;
            }

            _groupedClassPlanNodes.RemoveAt(i);
            _classPlanTreeGroupNodes.Remove(group.Guid);
            _classPlanTreeGroupChildren.Remove(group.Guid);
        }

        for (var targetIndex = 0; targetIndex < groupIds.Count; targetIndex++)
        {
            var groupId = groupIds[targetIndex];
            if (!_classPlanTreeGroupNodes.TryGetValue(groupId, out var group))
            {
                group = CreateClassPlanGroupNode(groupId);
                _classPlanTreeGroupNodes[groupId] = group;
            }

            var currentIndex = _groupedClassPlanNodes.IndexOf(group);
            if (currentIndex < 0)
            {
                _groupedClassPlanNodes.Insert(targetIndex, group);
            }
            else if (currentIndex != targetIndex)
            {
                _groupedClassPlanNodes.Move(currentIndex, targetIndex);
            }
        }
    }

    internal bool CanMoveClassPlan(Guid sourceId, Guid targetGroupId, Guid? targetClassPlanId, bool insertAfter)
    {
        return TryGetClassPlanMove(sourceId, targetGroupId, targetClassPlanId, insertAfter,
            out _, out _, out _);
    }

    internal bool MoveClassPlan(Guid sourceId, Guid targetGroupId, Guid? targetClassPlanId, bool insertAfter)
    {
        if (!TryGetClassPlanMove(sourceId, targetGroupId, targetClassPlanId, insertAfter,
                out var sourcePair, out var sourceIndex, out var insertIndex))
        {
            return false;
        }

        _suppressClassPlanTreeSynchronization = true;
        try
        {
            var classPlans = ProfileService.Profile.ClassPlans;
            classPlans.RemoveAt(sourceIndex);
            sourcePair.Value.AssociatedGroup = targetGroupId;
            classPlans.Insert(insertIndex, sourcePair);
        }
        finally
        {
            _suppressClassPlanTreeSynchronization = false;
            SynchronizeClassPlanTree();
        }

        SelectClassPlanByGuid(sourceId);
        return true;
    }

    private bool TryGetClassPlanMove(Guid sourceId, Guid targetGroupId, Guid? targetClassPlanId, bool insertAfter,
        out KeyValuePair<Guid, ClassPlan> sourcePair, out int sourceIndex, out int insertIndex)
    {
        sourcePair = default;
        sourceIndex = -1;
        insertIndex = -1;

        if (!_classPlanTreeGroupNodes.ContainsKey(targetGroupId))
        {
            return false;
        }

        var classPlans = ProfileService.Profile.ClassPlans;
        for (var i = 0; i < classPlans.Count; i++)
        {
            if (classPlans[i].Key != sourceId)
            {
                continue;
            }

            sourcePair = classPlans[i];
            sourceIndex = i;
            break;
        }

        if (sourceIndex < 0)
        {
            return false;
        }

        if (targetClassPlanId is { } targetId)
        {
            if (targetId == sourceId)
            {
                return false;
            }

            var targetIndex = -1;
            for (var i = 0; i < classPlans.Count; i++)
            {
                if (classPlans[i].Key == targetId)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex < 0 || classPlans[targetIndex].Value.AssociatedGroup != targetGroupId)
            {
                return false;
            }

            var targetIndexAfterRemoval = targetIndex > sourceIndex ? targetIndex - 1 : targetIndex;
            insertIndex = targetIndexAfterRemoval + (insertAfter ? 1 : 0);
        }
        else
        {
            if (sourcePair.Value.AssociatedGroup == targetGroupId)
            {
                var hasFollowingPlanInGroup = false;
                for (var i = sourceIndex + 1; i < classPlans.Count; i++)
                {
                    if (classPlans[i].Value.AssociatedGroup == targetGroupId)
                    {
                        hasFollowingPlanInGroup = true;
                        break;
                    }
                }

                if (!hasFollowingPlanInGroup)
                {
                    return false;
                }
            }

            var remainingIndex = 0;
            var lastTargetGroupIndex = -1;
            for (var i = 0; i < classPlans.Count; i++)
            {
                if (i == sourceIndex)
                {
                    continue;
                }

                if (classPlans[i].Value.AssociatedGroup == targetGroupId)
                {
                    lastTargetGroupIndex = remainingIndex;
                }

                remainingIndex++;
            }

            if (lastTargetGroupIndex >= 0)
            {
                insertIndex = lastTargetGroupIndex + 1;
            }
            else
            {
                var targetGroupIndex = _groupedClassPlanNodes.IndexOf(_classPlanTreeGroupNodes[targetGroupId]);
                insertIndex = classPlans.Count - 1;

                for (var groupIndex = targetGroupIndex + 1;
                     groupIndex < _groupedClassPlanNodes.Count;
                     groupIndex++)
                {
                    var nextGroupId = _groupedClassPlanNodes[groupIndex].Guid;
                    remainingIndex = 0;
                    var foundNextGroup = false;

                    for (var i = 0; i < classPlans.Count; i++)
                    {
                        if (i == sourceIndex)
                        {
                            continue;
                        }

                        if (classPlans[i].Value.AssociatedGroup == nextGroupId)
                        {
                            insertIndex = remainingIndex;
                            foundNextGroup = true;
                            break;
                        }

                        remainingIndex++;
                    }

                    if (foundNextGroup)
                    {
                        break;
                    }
                }
            }
        }

        return sourcePair.Value.AssociatedGroup != targetGroupId || sourceIndex != insertIndex;
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
