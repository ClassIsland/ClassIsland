using ClassIsland.Core.Models.Theming;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.ViewModels;
using CommunityToolkit.Mvvm.Input;
using unvell.ReoGrid;
using unvell.ReoGrid.Events;
using unvell.ReoGrid.Graphics;
using RangePosition = unvell.ReoGrid.RangePosition;
using System.Windows.Media.Animation;
using ClassIsland.Shared.Models.Profile;
using CsesSharp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using unvell.ReoGrid.DataFormat;
using unvell.ReoGrid.IO;
using Subject = ClassIsland.Shared.Models.Profile.Subject;
using unvell.ReoGrid.IO.OpenXML.Schema;
using CommonDialog = ClassIsland.Core.Controls.CommonDialog.CommonDialog;

namespace ClassIsland.Views;

/// <summary>
/// ExcelExportWindow.xaml 的交互逻辑
/// </summary>
public partial class ExcelExportWindow
{
    public IThemeService ThemeService { get; }
    public IProfileService ProfileService { get; }
    public ILogger<ExcelExportWindow> Logger { get; }

    public ExcelExportViewModel ViewModel { get; } = new();

    public ExcelExportWindow(IThemeService themeService, IProfileService profileService, ILogger<ExcelExportWindow> logger)
    {
        ThemeService = themeService;
        ProfileService = profileService;
        Logger = logger;
        InitializeComponent();
    }

    #region Theme

    private void ThemeServiceOnThemeUpdated(object? sender, ThemeUpdatedEventArgs e)
    {
        UpdateChartStyle();
    }

    private void UpdateChartStyle()
    {
        var rgcs = ControlAppearanceStyle.CreateDefaultControlStyle();
        var primary = (SolidColorBrush)FindResource("PrimaryHueMidBrush");
        var body = (SolidColorBrush)FindResource("MaterialDesignBody");
        var paper = (SolidColorBrush)FindResource("MaterialDesignPaper");
        var divider = (SolidColorBrush)FindResource("MaterialDesignDivider");
        var c = primary.Color;
        var sc = new SolidColor(c.R, c.G, c.B);
        var sca = SolidColor.FromArgb((byte)30, c.R, c.G, c.B);
        var sca2 = SolidColor.FromArgb((byte)180, c.R, c.G, c.B);
        var b = SolidColor.FromArgb(body.Color.R, body.Color.G, body.Color.B);
        var p = SolidColor.FromArgb(paper.Color.R, paper.Color.G, paper.Color.B);
        var d = SolidColor.FromArgb((byte)32, divider.Color.R, divider.Color.G, divider.Color.B);
        rgcs[ControlAppearanceColors.LeadHeadNormal] = p;
        rgcs[ControlAppearanceColors.LeadHeadHover] = p;
        rgcs[ControlAppearanceColors.LeadHeadSelected] = sc;
        rgcs[ControlAppearanceColors.LeadHeadIndicatorStart] = sc;
        rgcs[ControlAppearanceColors.LeadHeadIndicatorEnd] = sc;
        rgcs[ControlAppearanceColors.ColHeadSplitter] = d;
        rgcs[ControlAppearanceColors.ColHeadNormalStart] = p;
        rgcs[ControlAppearanceColors.ColHeadNormalEnd] = p;
        rgcs[ControlAppearanceColors.ColHeadHoverStart] = p;
        rgcs[ControlAppearanceColors.ColHeadHoverEnd] = p;
        rgcs[ControlAppearanceColors.ColHeadSelectedStart] = sc;
        rgcs[ControlAppearanceColors.ColHeadSelectedEnd] = sc;
        rgcs[ControlAppearanceColors.ColHeadFullSelectedStart] = sc;
        rgcs[ControlAppearanceColors.ColHeadFullSelectedEnd] = sc;
        rgcs[ControlAppearanceColors.ColHeadInvalidStart] = b;
        rgcs[ControlAppearanceColors.ColHeadInvalidEnd] = b;
        rgcs[ControlAppearanceColors.ColHeadText] = b;
        rgcs[ControlAppearanceColors.RowHeadSplitter] = d;
        rgcs[ControlAppearanceColors.RowHeadNormal] = p;
        rgcs[ControlAppearanceColors.RowHeadHover] = p;
        rgcs[ControlAppearanceColors.RowHeadSelected] = sc;
        rgcs[ControlAppearanceColors.RowHeadFullSelected] = sc;
        rgcs[ControlAppearanceColors.RowHeadInvalid] = b;
        rgcs[ControlAppearanceColors.RowHeadText] = b;
        rgcs[ControlAppearanceColors.SelectionBorder] = sca2;
        rgcs[ControlAppearanceColors.SelectionFill] = sca;
        rgcs[ControlAppearanceColors.GridBackground] = Color.FromArgb(255, 255, 255, 255);
        rgcs[ControlAppearanceColors.GridText] = Color.FromArgb(255, 0, 0, 0);
        rgcs[ControlAppearanceColors.GridLine] = Color.FromArgb(255, 208, 215, 229);
        Grid.ControlStyle = rgcs;
    }

    #endregion

    #region MenuActions

    private void MenuItemCancelCellMerge_OnClick(object sender, RoutedEventArgs e)
    {
        Grid.CurrentWorksheet.UnmergeRange(Grid.CurrentWorksheet.SelectionRange);
    }

    private void MenuItemCellMerge_OnClick(object sender, RoutedEventArgs e)
    {
        Grid.CurrentWorksheet.MergeRange(Grid.CurrentWorksheet.SelectionRange);
    }

    private void MenuItemCopy_OnClick(object sender, RoutedEventArgs e)
    {
        Grid.CurrentWorksheet.Copy();
    }

    private void MenuItemPaste_OnClick(object sender, RoutedEventArgs e)
    {
        Grid.CurrentWorksheet.Paste();
    }

    private void MenuItemCut_OnClick(object sender, RoutedEventArgs e)
    {
        Grid.CurrentWorksheet.Cut();
    }

    private void MenuItemClearContent_OnClick(object sender, RoutedEventArgs e)
    {
        Grid.CurrentWorksheet.ClearRangeContent(Grid.CurrentWorksheet.SelectionRange, CellElementFlag.All);
    }

    #endregion

    private void ExcelExportWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateChartStyle();
        ThemeService.ThemeUpdated += ThemeServiceOnThemeUpdated;

        PostRefreshWorksheet();
        ViewModel.SelectedClassPlanIds = new ObservableCollection<string>(ProfileService.Profile.ClassPlans
            .Where(x => x.Value is { IsEnabled: true, TimeRule.WeekDay: >= 1 and <= 5 })
            .Select(x => x.Key));
        GenerateClassPlanCells(false);

        ViewModel.SelectedClassPlanIds.CollectionChanged += SelectedClassPlanIdsOnCollectionChanged;
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        ViewModel.PropertyChanging += ViewModelOnPropertyChanging;
    }

    private void ViewModelOnPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModel.ClassPlanStartPos))
        {
            ClearGeneratedCells();
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModel.SelectedClassPlanIds)
                            or nameof(ViewModel.ClassPlanStartPos))
        {
            GenerateClassPlanCells(e.PropertyName != nameof(ViewModel.ClassPlanStartPos));
        }
    }

    private void SelectedClassPlanIdsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        GenerateClassPlanCells(true);
    }

    private void ExcelExportWindow_OnClosed(object? sender, EventArgs e)
    {
        ThemeService.ThemeUpdated -= ThemeServiceOnThemeUpdated;
    }

    private void ButtonUndo_OnClick(object sender, RoutedEventArgs e)
    {
        Grid.Undo();
    }

    private void ButtonRedo_OnClick(object sender, RoutedEventArgs e)
    {
        Grid.Redo();
    }

    private void RefreshGridState()
    {
        ViewModel.CanUndo = Grid.CanUndo();
        ViewModel.CanRedo = Grid.CanRedo();

        var range = Grid.CurrentWorksheet.SelectionRange;
        ViewModel.SelectedPosition = range;
        if (range == RangePosition.Empty)
        {
            ViewModel.SelectedCell = null;
            return;  // 跳过单元格样式刷新
        }
        var cell = Grid.CurrentWorksheet.Cells[range.StartPos];
        ViewModel.SelectedRange = Grid.CurrentWorksheet.Ranges[range];
        ViewModel.SelectedCell = cell;
    }

    private void PostRefreshWorksheet()
    {
        ViewModel.CurrentWorksheet = Grid.CurrentWorksheet;
        ViewModel.CurrentWorksheet.SelectionRangeChanged += CurrentWorksheetOnSelectionRangeChanged;

        RefreshGridState();
    }

    private void PreRefreshWorksheet()
    {
        if (ViewModel.CurrentWorksheet == null)
        {
            return;
        }

        ViewModel.CurrentWorksheet.SelectionRangeChanged -= CurrentWorksheetOnSelectionRangeChanged;
        ViewModel.SelectedRange = null;
        ViewModel.SelectedCell = null;
        ViewModel.SelectedPosition = RangePosition.Empty;
    } 

    private void CurrentWorksheetOnSelectionRangeChanged(object? sender, RangeEventArgs e)
    {
        RefreshGridState();
    }

    private void Grid_OnActionPerformed(object? sender, WorkbookActionEventArgs e)
    {
        RefreshGridState();
    }

    private void Grid_OnCurrentWorksheetChanged(object? sender, EventArgs e)
    {
        PreRefreshWorksheet();
        PostRefreshWorksheet();
    }

    private void Grid_OnWorkbookLoaded(object? sender, EventArgs e)
    {
        PostRefreshWorksheet();
    }

    private void UpdateRangeStyle()
    {
    }

    private void SelectorFontFormat_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateRangeStyle();
    }

    private void ButtonBaseFontSizeDecrease_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRange != null)
        {
            ViewModel.SelectedRange.Style.FontSize -= 1;
        }
        RefreshGridState();
    }

    private void ButtonBaseFontSizeIncrease_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRange != null)
        {
            ViewModel.SelectedRange.Style.FontSize += 1;
        }
        RefreshGridState();
    }

    private void ButtonMergeCells_OnClick(object sender, RoutedEventArgs e)
    {
        Grid.CurrentWorksheet.MergeRange(ViewModel.SelectedRange);
    }

    private void ButtonUnMergeCells_OnClick(object sender, RoutedEventArgs e)
    {
        Grid.CurrentWorksheet.UnmergeRange(ViewModel.SelectedRange);
    }

    [RelayCommand]
    private void EnterSelectingMode(string propertyName)
    {
        ViewModel.CurrentUpdatingPropertyName = propertyName;
        //ViewModel.CurrentSelectingElement = (ExcelSelectionTextBox)sender;
        //BeginStoryboard((Storyboard)FindResource("EditingLoop"));
        ViewModel.IsSelectingMode = true;
        ViewModel.NormalSelectionRangePosition = Grid.CurrentWorksheet.SelectionRange;
        var s = typeof(ExcelExportViewModel).GetProperty(ViewModel.CurrentUpdatingPropertyName)?.GetValue(ViewModel);
        ViewModel.SelectedRangePosition = Grid.CurrentWorksheet.SelectionRange
            = (RangePosition)(s ?? RangePosition.Empty);
        Grid.CurrentWorksheet.SelectionRangeChanged += CurrentWorksheetOnSelectionRangeChanged_SelectingMode;
        if (ViewModel.CurrentSelectingElement != null) ViewModel.CurrentSelectingElement.IsSelecting = true;
        Grid.ControlStyle.SelectionBorderWidth = 5;
    }

    private void CurrentWorksheetOnSelectionRangeChanged_SelectingMode(object? sender, RangeEventArgs e)
    {
        ViewModel.SelectedRangePosition = e.Range;
        var vmt = typeof(ExcelExportViewModel);
        var p = vmt.GetProperty(ViewModel.CurrentUpdatingPropertyName);
        if (p == null)
        {
            return;
        }

        p.SetValue(ViewModel, e.Range);
        ViewModel.CurrentSelectingElement?.Focus();
    }

    [RelayCommand]
    private void ExitSelectingMode()
    {
        ViewModel.IsSelectingMode = false;
        Grid.CurrentWorksheet.SelectionRangeChanged -= CurrentWorksheetOnSelectionRangeChanged_SelectingMode;
        Grid.ControlStyle.SelectionBorderWidth = 3;
        Grid.CurrentWorksheet.SelectionRange = ViewModel.NormalSelectionRangePosition;
        if (ViewModel.CurrentSelectingElement != null) ViewModel.CurrentSelectingElement.IsSelecting = false;
        ViewModel.CurrentSelectingElement = null;
        //((Storyboard)FindResource("EditingLoop")).Stop();
        //((Storyboard)FindResource("EditingLoop")).Remove();
    }

    private string GetWeekDayName(int x) => x switch
    {
        0 => "星期日",
        1 => "星期一",
        2 => "星期二",
        3 => "星期三",
        4 => "星期四",
        5 => "星期五",
        6 => "星期六",
        _ => "???",
    };

    private void GenerateClassPlanCells(bool clear)
    {
        if (clear)
        {
            ClearGeneratedCells();
        }

        List<ClassPlan> classPlans = [];
        foreach (var i in ViewModel.SelectedClassPlanIds)
        {
            if (ProfileService.Profile.ClassPlans.TryGetValue(i, out var cp))
            {
                classPlans.Add(cp);
            }
        }
        classPlans.Sort((x, y) => x.TimeRule.WeekDay - y.TimeRule.WeekDay);

        List<List<string>?> cellData = [null, null, null, null, null, null, null];
        for (int i = 0; i < 7; i++)
        {
            var currentCp = classPlans.Where(x => x.TimeRule.WeekDay == i).ToList();
            if (currentCp.Count <= 0)
            {
                continue;
            }

            var maxIndex = currentCp.Select(x => x.Classes.Count).Max();
            List<string> data = [GetWeekDayName(i)];
            for (int j = 0; j < maxIndex; j++)
            {
                var isEqual = true;
                List<string> subjects = [];
                foreach (var k in currentCp.Where(k => j < k.Classes.Count))
                {
                    subjects.Add(k.Classes[j].SubjectId);

                    if (subjects[0] == k.Classes[j].SubjectId)
                    {
                        continue;
                    }

                    isEqual = false;
                    break;
                }

                if (subjects.Count <= 0)
                {
                    break;
                }

                var subjectObjects = subjects.Select(x => ProfileService.Profile.Subjects[x] ?? Subject.Empty).ToList();
                var textLine1 = isEqual ? $"{subjectObjects[0].Name}"
                    : $"{string.Join("/", subjectObjects.Select(x => x.Name))}";
                var textLine2 = isEqual
                    ? $"\n{subjectObjects[0].TeacherName}"
                    : $"\n{string.Join("/", subjectObjects.Select(x => x.TeacherName))}";
                data.Add(textLine1 + (ViewModel.IsTeacherNameEnabled ? textLine2 : string.Empty));
            }

            cellData[i] = data;
        }

        var colBase = ViewModel.ClassPlanStartPos.Col;
        var col = colBase + 1;
        var rowBase = ViewModel.ClassPlanStartPos.Row;
        foreach (var i in cellData)
        {
            if (i == null)
            {
                continue;
            }

            var row = rowBase;
            foreach (var j in i)
            {
                var pos = new CellPosition(row, col);
                Grid.CurrentWorksheet.Cells[pos].Data = j;
                Grid.CurrentWorksheet.Cells[pos].DataFormat = CellDataFormatFlag.Text;
                row++;
            }

            ViewModel.GeneratedRowsCount = Math.Max(ViewModel.GeneratedRowsCount, row - rowBase);
            col++;
        }

        for (int i = 1; i <= ViewModel.GeneratedRowsCount - 1; i++)
        {
            var pos = new CellPosition(rowBase + i, colBase);
            Grid.CurrentWorksheet.Cells[pos].Data = i.ToString();
            Grid.CurrentWorksheet.Cells[pos].DataFormat = CellDataFormatFlag.Text;
        }

        ViewModel.GeneratedColsCount = col - colBase;

        var range = new RangePosition(ViewModel.ClassPlanStartPos.Row, ViewModel.ClassPlanStartPos.Col, ViewModel.GeneratedRowsCount,
            ViewModel.GeneratedColsCount);
        var style = Grid.CurrentWorksheet.Ranges[range].Style;
        style.HorizontalAlign = ViewModel.HorAlign;
        style.VerticalAlign = ViewModel.VerAlign;
        style.TextColor = SolidColor.FromArgb((byte)255, ViewModel.ForegroundColor.R, ViewModel.ForegroundColor.G,
            ViewModel.ForegroundColor.B);
        style.BackColor = SolidColor.FromArgb((byte)255, ViewModel.BackgroundColor.R, ViewModel.BackgroundColor.G,
            ViewModel.BackgroundColor.B);
        style.TextWrap = TextWrapMode.BreakAll;
        Grid.CurrentWorksheet.SetRowsHeight(rowBase, ViewModel.GeneratedRowsCount, (ushort)ViewModel.RowHeight);
        Grid.CurrentWorksheet.SetColumnsWidth(colBase + 1, ViewModel.GeneratedColsCount, (ushort)ViewModel.ColWidth);
        Grid.CurrentWorksheet.SetColumnsWidth(colBase, 1, 32);
        Grid.CurrentWorksheet.SetRangeBorders(range, ViewModel.BorderMode switch
        {
            0 => BorderPositions.None,
            1 => BorderPositions.All,
            2 => BorderPositions.All,
            _ => BorderPositions.None
        },
            ViewModel.BorderMode switch
            {
                0 => RangeBorderStyle.Empty,
                1 => RangeBorderStyle.BlackSolid,
                2 => RangeBorderStyle.BlackBoldSolid,
                _ => RangeBorderStyle.Empty
            }); 
    }

    private void ClearGeneratedCells()
    {
        var startPos = ViewModel.ClassPlanStartPos;
        Grid.CurrentWorksheet.ClearRangeContent(new RangePosition(startPos.Row, startPos.Col, ViewModel.GeneratedRowsCount, ViewModel.GeneratedColsCount), CellElementFlag.All);
    }

    private void ButtonRegenerate_OnClick(object sender, RoutedEventArgs e)
    {
        GenerateClassPlanCells(true);
    }

    private string? SaveFile()
    {
        var dialog = new SaveFileDialog()
        {
            Title = "保存课表表格文件",
            Filter = $"Excel 工作簿(*.xlsx)|*.xlsx"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return null;
        }

        Grid.Save(dialog.FileName, FileFormat.Excel2007, Encoding.Default);
        ViewModel.MessageQueue.Enqueue($"已保存到 {dialog.FileName}");
        return dialog.FileName;
    }

    private void ButtonSaveToFile_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveFile();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法保存 Excel 表格");
            CommonDialog.ShowError($"无法保存文件：{exception.Message}");
        }
    }

    private void ButtonSaveAndOpenInExcel_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var filename = SaveFile();
            if (filename == null)
            {
                return;
            }

            Process.Start(new ProcessStartInfo(filename)
            {
                UseShellExecute = true
            });
            Close();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法保存并打开 Excel 表格");
            CommonDialog.ShowError($"无法保存并打开文件：{exception.Message}");
        }
    }
}