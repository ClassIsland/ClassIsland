using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using ClassIsland.Controls;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Models;

using CommunityToolkit.Mvvm.ComponentModel;

using unvell.ReoGrid;

namespace ClassIsland.ViewModels;

public class ExcelImportViewModel : ObservableRecipient
{
    private int _slideIndex = 0;
    private int _descriptionMode = 0;
    private bool _isLoadingExcelFile = false;
    private bool _isFileSelected = false;
    private bool _isSelectingMode = false;
    private RangePosition _selectedRangePosition = RangePosition.Empty;
    private string _currentUpdatingPropertyName = "";
    private RangePosition _testSelectingRangePosition = RangePosition.Empty;
    private RangePosition _normalSelectionRangePosition = RangePosition.Empty;
    private ExcelSelectionTextBox? _currentSelectingElement = null;
    private RangePosition _testSelectingRangePosition2 = RangePosition.Empty;
    private RangePosition _timePointSourcePosition = RangePosition.Empty;
    private int _timeLayoutImportSource = -1;
    private RangePosition _subjectSourcePosition = RangePosition.Empty;
    private TimeLayout _selectedTimeLayout = new TimeLayout();
    private string _selectedTimeLayoutId = "";
    private bool _isVerticalLayout = false;
    private ObservableCollection<RangePosition> _classPlanSources = new();
    private RangePosition _currentClassPlanSource = new RangePosition();
    private ObservableCollection<Selectable<RangePosition>> _classRecognitionRange = new();
    private ObservableCollection<Selectable<TimeLayoutItem>> _classRecognitionTimePoints = new();
    private ObservableCollection<TimeLayoutItem> _classRecognitionTimePointsAll = new();
    private ObservableCollection<RangePosition> _classRecognitionRangeAll = new();
    private List<int> _classRecognitionRangeCache = new();
    private ClassPlan _currentClassPlan = new();
    private ObservableCollection<ClassPlan> _importedClassPlans = new();
    private bool _isTimeLayoutRangeValid = false;
    private bool _isSubjectsSourceRangeValid = false;
    private bool _isDragEntering = false;
    private string _openFileName = "";
    private Exception _openException = new();

    public int SlideIndex
    {
        get => _slideIndex;
        set
        {
            if (value == _slideIndex) return;
            _slideIndex = value;
            OnPropertyChanged();
        }
    }

    public int DescriptionMode
    {
        get => _descriptionMode;
        set
        {
            if (value == _descriptionMode) return;
            _descriptionMode = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoadingExcelFile
    {
        get => _isLoadingExcelFile;
        set
        {
            if (value == _isLoadingExcelFile) return;
            _isLoadingExcelFile = value;
            OnPropertyChanged();
        }
    }

    public bool IsFileSelected
    {
        get => _isFileSelected;
        set
        {
            if (value == _isFileSelected) return;
            _isFileSelected = value;
            OnPropertyChanged();
        }
    }

    public bool IsSelectingMode
    {
        get => _isSelectingMode;
        set
        {
            if (value == _isSelectingMode) return;
            _isSelectingMode = value;
            OnPropertyChanged();
        }
    }

    public RangePosition SelectedRangePosition
    {
        get => _selectedRangePosition;
        set
        {
            if (value.Equals(_selectedRangePosition)) return;
            _selectedRangePosition = value;
            OnPropertyChanged();
        }
    }

    public string CurrentUpdatingPropertyName
    {
        get => _currentUpdatingPropertyName;
        set
        {
            if (value == _currentUpdatingPropertyName) return;
            _currentUpdatingPropertyName = value;
            OnPropertyChanged();
        }
    }

    public RangePosition TestSelectingRangePosition
    {
        get => _testSelectingRangePosition;
        set
        {
            if (value.Equals(_testSelectingRangePosition)) return;
            _testSelectingRangePosition = value;
            OnPropertyChanged();
        }
    }

    public RangePosition TestSelectingRangePosition2
    {
        get => _testSelectingRangePosition2;
        set
        {
            if (value.Equals(_testSelectingRangePosition2)) return;
            _testSelectingRangePosition2 = value;
            OnPropertyChanged();
        }
    }

    public RangePosition NormalSelectionRangePosition
    {
        get => _normalSelectionRangePosition;
        set
        {
            if (value.Equals(_normalSelectionRangePosition)) return;
            _normalSelectionRangePosition = value;
            OnPropertyChanged();
        }
    }

    public ExcelSelectionTextBox? CurrentSelectingElement
    {
        get => _currentSelectingElement;
        set
        {
            if (Equals(value, _currentSelectingElement)) return;
            _currentSelectingElement = value;
            OnPropertyChanged();
        }
    }

    public RangePosition TimePointSourcePosition
    {
        get => _timePointSourcePosition;
        set
        {
            if (value.Equals(_timePointSourcePosition)) return;
            _timePointSourcePosition = value;
            OnPropertyChanged();
        }
    }

    public int TimeLayoutImportSource
    {
        get => _timeLayoutImportSource;
        set
        {
            if (value == _timeLayoutImportSource) return;
            _timeLayoutImportSource = value;
            OnPropertyChanged();
        }
    }

    public RangePosition SubjectSourcePosition
    {
        get => _subjectSourcePosition;
        set
        {
            if (value.Equals(_subjectSourcePosition)) return;
            _subjectSourcePosition = value;
            OnPropertyChanged();
        }
    }

    public TimeLayout SelectedTimeLayout
    {
        get => _selectedTimeLayout;
        set
        {
            if (Equals(value, _selectedTimeLayout)) return;
            _selectedTimeLayout = value;
            OnPropertyChanged();
        }
    }

    public string SelectedTimeLayoutId
    {
        get => _selectedTimeLayoutId;
        set
        {
            if (value == _selectedTimeLayoutId) return;
            _selectedTimeLayoutId = value;
            OnPropertyChanged();
        }
    }

    public bool IsVerticalLayout
    {
        get => _isVerticalLayout;
        set
        {
            if (value == _isVerticalLayout) return;
            _isVerticalLayout = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<RangePosition> ClassPlanSources
    {
        get => _classPlanSources;
        set
        {
            if (Equals(value, _classPlanSources)) return;
            _classPlanSources = value;
            OnPropertyChanged();
        }
    }

    public RangePosition CurrentClassPlanSource
    {
        get => _currentClassPlanSource;
        set
        {
            if (value.Equals(_currentClassPlanSource)) return;
            _currentClassPlanSource = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Selectable<RangePosition>> ClassRecognitionRange
    {
        get => _classRecognitionRange;
        set
        {
            if (Equals(value, _classRecognitionRange)) return;
            _classRecognitionRange = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Selectable<TimeLayoutItem>> ClassRecognitionTimePoints
    {
        get => _classRecognitionTimePoints;
        set
        {
            if (Equals(value, _classRecognitionTimePoints)) return;
            _classRecognitionTimePoints = value;
            OnPropertyChanged();
        }
    }

    public List<int> ClassRecognitionRangeCache
    {
        get => _classRecognitionRangeCache;
        set
        {
            if (Equals(value, _classRecognitionRangeCache)) return;
            _classRecognitionRangeCache = value;
            OnPropertyChanged();
        }
    }

    public ClassPlan CurrentClassPlan
    {
        get => _currentClassPlan;
        set
        {
            if (Equals(value, _currentClassPlan)) return;
            _currentClassPlan = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ClassPlan> ImportedClassPlans
    {
        get => _importedClassPlans;
        set
        {
            if (Equals(value, _importedClassPlans)) return;
            _importedClassPlans = value;
            OnPropertyChanged();
        }
    }

    public bool IsTimeLayoutRangeValid
    {
        get => _isTimeLayoutRangeValid;
        set
        {
            if (value == _isTimeLayoutRangeValid) return;
            _isTimeLayoutRangeValid = value;
            OnPropertyChanged();
        }
    }

    public bool IsSubjectsSourceRangeValid
    {
        get => _isSubjectsSourceRangeValid;
        set
        {
            if (value == _isSubjectsSourceRangeValid) return;
            _isSubjectsSourceRangeValid = value;
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

    public string OpenFileName
    {
        get => _openFileName;
        set
        {
            if (value == _openFileName) return;
            _openFileName = value;
            OnPropertyChanged();
        }
    }

    public Exception OpenException
    {
        get => _openException;
        set
        {
            if (Equals(value, _openException)) return;
            _openException = value;
            OnPropertyChanged();
        }
    }

    public Guid DialogId { get; } = Guid.NewGuid();

}