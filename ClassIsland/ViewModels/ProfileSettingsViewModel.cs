using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Core.Models.UI;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;


namespace ClassIsland.ViewModels;

public partial class ProfileSettingsViewModel : ObservableRecipient
{
    public IProfileService ProfileService { get; }
    public IManagementService ManagementService { get; }
    public SettingsService SettingsService { get; }
    public ILessonsService LessonsService { get; }
    public IExactTimeService ExactTimeService { get; }
    public IActionService ActionService { get; }
    public ILogger<ProfileSettingsWindow> Logger { get; }

    public SyncDictionaryList<Guid, ClassPlan> ClassPlans { get; }
    public SyncDictionaryList<Guid, TimeLayout> TimeLayouts { get; }
    public SyncDictionaryList<Guid, Subject> Subjects { get; }

    public SyncDictionaryList<Guid, ClassPlanGroup> ClassPlanGroups { get; }
    public SyncDictionaryList<DateTime, OrderedSchedule> OrderedSchedules { get; }

    public IObservableList<KeyValuePair<Guid, ClassPlan>> TempClassPlanList { get; }


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
    [ObservableProperty] private ObservableCollection<WeekClassPlanRow> _weekClassPlanRows = [];
    [ObservableProperty] private bool _isProfileImportMenuOpened = false;
    [ObservableProperty] private bool _isInScheduleSwappingMode = false;
    [ObservableProperty] private WeekClassPlanRow? _selectedWeekClassPlanRow;
    [ObservableProperty] private ScheduleClassPosition _classSwapEndPosition = ScheduleClassPosition.Zero;
    [ObservableProperty] private ScheduleClassPosition _classSwapStartPosition = ScheduleClassPosition.Zero;
    [ObservableProperty] private DateTime _scheduleWeekViewBaseDate = DateTime.Now;
    [ObservableProperty] private bool _isTempSwapMode = true;
    [ObservableProperty] private int _dataGridWeekRowsWeekIndex = 0;
    [ObservableProperty] private bool _isClassPlanTempEditPopupOpen = false;
    [ObservableProperty] private Guid _targetSubjectIndex = Guid.Empty;
    [ObservableProperty] private bool _isTimeLineSticky = true;
    [ObservableProperty] private bool _isDrawerOpen = false;
    [ObservableProperty] private int _masterPageTabSelectIndex = 0;
    [ObservableProperty] private TimeLayout? _selectedTimeLayout;
    [ObservableProperty] private int _selectedTimePointIndex = -1;
    [ObservableProperty] private ToastMessage? _currentTimePointDeleteRevertToast;
    [ObservableProperty] private ToastMessage? _currentClassPlanEditDoneToast = null;
    [ObservableProperty] private KeyValuePair<Guid, TimeLayout>? _classPlanInfoSelectedTimeLayoutKvp;

/// <inheritdoc/>
    public ProfileSettingsViewModel(IProfileService profileService, IManagementService managementService,
        SettingsService settingsService, ILessonsService lessonsService, IExactTimeService exactTimeService,
        IActionService actionService,
        ILogger<ProfileSettingsWindow> logger)
    {
        ProfileService = profileService;
        ManagementService = managementService;
        SettingsService = settingsService;
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;
        ActionService = actionService;
        Logger = logger;

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
    }
}