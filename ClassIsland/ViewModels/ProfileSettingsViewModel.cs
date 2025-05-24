using System;
using System.Collections.ObjectModel;
using ClassIsland.Models;
using ClassIsland.Shared.Models.Profile;

using CommunityToolkit.Mvvm.ComponentModel;

using MaterialDesignThemes.Wpf;

namespace ClassIsland.ViewModels;

public class ProfileSettingsViewModel : ObservableRecipient
{
    private object _drawerContent = new();
    private bool _isClassPlansEditing = false;
    private SnackbarMessageQueue _messageQueue = new();
    private ObservableCollection<string> _profiles = new();
    private bool _isRestartSnackbarActive = false;
    private string _renameProfileName = "";
    private string _createProfileName = "";
    private string _selectedProfile = "";
    private string _deleteConfirmField = "";
    private bool _isOfflineEditor = false;
    private TimeLayoutItem? _selectedTimePoint;
    private double _timeLineScale = 3.0;
    private Subject? _selectedSubject;
    private bool _isPanningModeEnabled = false;
    private bool _isDragEntering = false;
    private string _tempOverlayClassPlanTimeLayoutId = "";
    private ClassInfo?  _selectedClassInfo;
    private int _selectedClassIndex = -1;
    private ClassPlan _selectedClassPlan = new();
    private bool _isUpdatingClassInfoIndexInBackend = false;
    private bool _isClassPlanEditComplete = false;
    private bool _isWeekOffsetSettingsOpen = false;
    private TimeLayoutItem? _previousTrackedTimeLayoutItem;
    private DateTime _scheduleCalendarSelectedDate = DateTime.Today;
    private DateTime _overlayEnableDateTime = DateTime.Today;
    private ObservableCollection<WeekClassPlanRow> _weekClassPlanRows = [];
    private bool _isProfileImportMenuOpened = false;
    private bool _isInScheduleSwappingMode = false;
    private WeekClassPlanRow? _selectedWeekClassPlanRow;
    private ScheduleClassPosition _classSwapEndPosition = ScheduleClassPosition.Zero;
    private ScheduleClassPosition _classSwapStartPosition = ScheduleClassPosition.Zero;
    private DateTime _scheduleWeekViewBaseDate = DateTime.Now;
    private bool _isTempSwapMode = true;
    private int _dataGridWeekRowsWeekIndex = 0;
    private bool _isClassPlanTempEditPopupOpen = false;
    private string _targetSubjectIndex = "";
    private bool _isTimeLineSticky = true;

    public object DrawerContent
    {
        get => _drawerContent;
        set
        {
            if (Equals(value, _drawerContent)) return;
            _drawerContent = value;
            OnPropertyChanged();
        }
    }

    public bool IsClassPlansEditing
    {
        get => _isClassPlansEditing;
        set
        {
            if (value == _isClassPlansEditing) return;
            _isClassPlansEditing = value;
            OnPropertyChanged();
        }
    }

    public SnackbarMessageQueue MessageQueue
    {
        get => _messageQueue;
        set
        {
            if (Equals(value, _messageQueue)) return;
            _messageQueue = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> Profiles
    {
        get => _profiles;
        set
        {
            if (Equals(value, _profiles)) return;
            _profiles = value;
            OnPropertyChanged();
        }
    }

    public bool IsRestartSnackbarActive
    {
        get => _isRestartSnackbarActive;
        set
        {
            if (value == _isRestartSnackbarActive) return;
            _isRestartSnackbarActive = value;
            OnPropertyChanged();
        }
    }

    public string CreateProfileName
    {
        get => _createProfileName;
        set
        {
            if (value == _createProfileName) return;
            _createProfileName = value;
            OnPropertyChanged();
        }
    }

    public string RenameProfileName
    {
        get => _renameProfileName;
        set
        {
            if (value == _renameProfileName) return;
            _renameProfileName = value;
            OnPropertyChanged();
        }
    }

    public string SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (value == _selectedProfile) return;
            _selectedProfile = value;
            OnPropertyChanged();
        }
    }

    public string DeleteConfirmField
    {
        get => _deleteConfirmField;
        set
        {
            if (value == _deleteConfirmField) return;
            _deleteConfirmField = value;
            OnPropertyChanged();
        }
    }

    public bool IsOfflineEditor
    {
        get => _isOfflineEditor;
        set
        {
            if (value == _isOfflineEditor) return;
            _isOfflineEditor = value;
            OnPropertyChanged();
        }
    }

    public Guid DialogHostId
    {
        get;
    } = Guid.NewGuid();

    public TimeLayoutItem? SelectedTimePoint
    {
        get => _selectedTimePoint;
        set
        {
            if (Equals(value, _selectedTimePoint)) return;
            _selectedTimePoint = value;
            OnPropertyChanged();
        }
    }

    public double TimeLineScale
    {
        get => _timeLineScale;
        set
        {
            if (value.Equals(_timeLineScale)) return;
            _timeLineScale = value;
            OnPropertyChanged();
        }
    }

    public Subject? SelectedSubject
    {
        get => _selectedSubject;
        set
        {
            if (Equals(value, _selectedSubject)) return;
            _selectedSubject = value;
            OnPropertyChanged();
        }
    }

    public bool IsPanningModeEnabled
    {
        get => _isPanningModeEnabled;
        set
        {
            if (value == _isPanningModeEnabled) return;
            _isPanningModeEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsDragEntering
    {
        get => _isDragEntering;
        set
        {
            if (value == _isDragEntering) return;
            _isDragEntering = value;
            OnPropertyChanged();
        }
    }

    public string TempOverlayClassPlanTimeLayoutId
    {
        get => _tempOverlayClassPlanTimeLayoutId;
        set
        {
            if (value == _tempOverlayClassPlanTimeLayoutId) return;
            _tempOverlayClassPlanTimeLayoutId = value;
            OnPropertyChanged();
        }
    }

    public ClassInfo? SelectedClassInfo
    {
        get => _selectedClassInfo;
        set
        {
            if (Equals(value,  _selectedClassInfo)) return;
             _selectedClassInfo = value;
            OnPropertyChanged();
        }
    }

    public int SelectedClassIndex
    {
        get => _selectedClassIndex;
        set
        {
            if (value == _selectedClassIndex) return;
            _selectedClassIndex = value;
            OnPropertyChanged();
        }
    }

    public ClassPlan SelectedClassPlan
    {
        get => _selectedClassPlan;
        set
        {
            if (Equals(value, _selectedClassPlan)) return;
            _selectedClassPlan = value;
            OnPropertyChanged();
        }
    }

    public bool IsUpdatingClassInfoIndexInBackend
    {
        get => _isUpdatingClassInfoIndexInBackend;
        set
        {
            if (value == _isUpdatingClassInfoIndexInBackend) return;
            _isUpdatingClassInfoIndexInBackend = value;
            OnPropertyChanged();
        }
    }

    public bool IsClassPlanEditComplete
    {
        get => _isClassPlanEditComplete;
        set
        {
            if (value == _isClassPlanEditComplete) return;
            _isClassPlanEditComplete = value;
            OnPropertyChanged();
        }
    }

    public bool IsWeekOffsetSettingsOpen
    {
        get => _isWeekOffsetSettingsOpen;
        set
        {
            if (value == _isWeekOffsetSettingsOpen) return;
            _isWeekOffsetSettingsOpen = value;
            OnPropertyChanged();
        }
    }

    public TimeLayoutItem? PreviousTrackedTimeLayoutItem
    {
        get => _previousTrackedTimeLayoutItem;
        set
        {
            if (Equals(value, _previousTrackedTimeLayoutItem)) return;
            _previousTrackedTimeLayoutItem = value;
            OnPropertyChanged();
        }
    }

    public DateTime ScheduleCalendarSelectedDate
    {
        get => _scheduleCalendarSelectedDate;
        set
        {
            if (value.Equals(_scheduleCalendarSelectedDate)) return;
            _scheduleCalendarSelectedDate = value;
            OnPropertyChanged();
        }
    }

    public DateTime OverlayEnableDateTime
    {
        get => _overlayEnableDateTime;
        set
        {
            if (value.Equals(_overlayEnableDateTime)) return;
            _overlayEnableDateTime = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<WeekClassPlanRow> WeekClassPlanRows
    {
        get => _weekClassPlanRows;
        set
        {
            if (Equals(value, _weekClassPlanRows)) return;
            _weekClassPlanRows = value;
            OnPropertyChanged();
        }
    }

    public bool IsProfileImportMenuOpened
    {
        get => _isProfileImportMenuOpened;
        set
        {
            if (value == _isProfileImportMenuOpened) return;
            _isProfileImportMenuOpened = value;
            OnPropertyChanged();
        }
    }

    public bool IsInScheduleSwappingMode
    {
        get => _isInScheduleSwappingMode;
        set
        {
            if (value == _isInScheduleSwappingMode) return;
            _isInScheduleSwappingMode = value;
            OnPropertyChanged();
        }
    }

    public WeekClassPlanRow? SelectedWeekClassPlanRow
    {
        get => _selectedWeekClassPlanRow;
        set
        {
            if (Equals(value, _selectedWeekClassPlanRow)) return;
            _selectedWeekClassPlanRow = value;
            OnPropertyChanged();
        }
    }

    public ScheduleClassPosition ClassSwapStartPosition
    {
        get => _classSwapStartPosition;
        set
        {
            if (Equals(value, _classSwapStartPosition)) return;
            _classSwapStartPosition = value;
            OnPropertyChanged();
        }
    }

    public ScheduleClassPosition ClassSwapEndPosition
    {
        get => _classSwapEndPosition;
        set
        {
            if (Equals(value, _classSwapEndPosition)) return;
            _classSwapEndPosition = value;
            OnPropertyChanged();
        }
    }

    public DateTime ScheduleWeekViewBaseDate
    {
        get => _scheduleWeekViewBaseDate;
        set
        {
            if (value.Equals(_scheduleWeekViewBaseDate)) return;
            _scheduleWeekViewBaseDate = value;
            OnPropertyChanged();
        }
    }

    public bool IsTempSwapMode
    {
        get => _isTempSwapMode;
        set
        {
            if (value == _isTempSwapMode) return;
            _isTempSwapMode = value;
            OnPropertyChanged();
        }
    }

    public int DataGridWeekRowsWeekIndex
    {
        get => _dataGridWeekRowsWeekIndex;
        set
        {
            if (value == _dataGridWeekRowsWeekIndex) return;
            _dataGridWeekRowsWeekIndex = value;
            OnPropertyChanged();
        }
    }

    public bool IsClassPlanTempEditPopupOpen
    {
        get => _isClassPlanTempEditPopupOpen;
        set
        {
            if (value == _isClassPlanTempEditPopupOpen) return;
            _isClassPlanTempEditPopupOpen = value;
            OnPropertyChanged();
        }
    }

    public string TargetSubjectIndex
    {
        get => _targetSubjectIndex;
        set
        {
            if (value == _targetSubjectIndex) return;
            _targetSubjectIndex = value;
            OnPropertyChanged();
        }
    }

    public bool IsTimeLineSticky
    {
        get => _isTimeLineSticky;
        set
        {
            if (value == _isTimeLineSticky) return;
            _isTimeLineSticky = value;
            OnPropertyChanged();
        }
    }
}