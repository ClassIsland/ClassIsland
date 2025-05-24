using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Media;
using ClassIsland.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;
using unvell.ReoGrid;
using unvell.ReoGrid.IO.OpenXML.Schema;
using Cell = unvell.ReoGrid.Cell;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Worksheet = unvell.ReoGrid.Worksheet;

namespace ClassIsland.ViewModels;

public partial class ExcelExportViewModel : ObservableObject
{

    [ObservableProperty] private bool _canUndo = false;

    [ObservableProperty] private bool _canRedo = false;

    [ObservableProperty] private RangePosition _selectedPosition = RangePosition.Empty;

    [ObservableProperty] private Cell? _selectedCell;

    public List<string> FontFamilies { get; } = Fonts.SystemFontFamilies.Where(x => x.FamilyNames.Count > 0).Select(x => x.FamilyNames.FirstOrDefault().Value).ToList();

    [ObservableProperty] private Worksheet? _currentWorksheet;

    [ObservableProperty] private ReferenceRange? _selectedRange;

    [ObservableProperty] private RangePosition _classPlanStartPos = new RangePosition(1, 0, 1, 1);

    [ObservableProperty] private bool _isSelectingMode = false;

    [ObservableProperty] private RangePosition _selectedRangePosition = RangePosition.Empty;

    [ObservableProperty] private string _currentUpdatingPropertyName = "";

    [ObservableProperty] private RangePosition _normalSelectionRangePosition = RangePosition.Empty;

    [ObservableProperty] private ExcelSelectionTextBox? _currentSelectingElement = null;

    [ObservableProperty] private ObservableCollection<string> _selectedClassPlanIds = [];

    [ObservableProperty] private int _generatedRowsCount = 0;

    [ObservableProperty] private int _generatedColsCount = 0;

    [ObservableProperty] private bool _isTeacherNameEnabled = true;

    [ObservableProperty] private ReoGridHorAlign _horAlign = ReoGridHorAlign.Center;

    [ObservableProperty] private ReoGridVerAlign _verAlign = ReoGridVerAlign.Top;

    [ObservableProperty] private bool _isForegroundColorEnabled = false;

    [ObservableProperty] private Color _foregroundColor = Colors.Black;

    [ObservableProperty] private bool _isBackgroundColorEnabled = false;

    [ObservableProperty] private Color _backgroundColor = Colors.White;

    [ObservableProperty] private int _borderMode = 1;

    [ObservableProperty] private double _rowHeight = 40.0;

    [ObservableProperty] private double _colWidth = 100.0;

    [ObservableProperty] private SnackbarMessageQueue _messageQueue = new();
}