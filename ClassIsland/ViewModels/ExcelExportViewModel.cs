using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
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
}