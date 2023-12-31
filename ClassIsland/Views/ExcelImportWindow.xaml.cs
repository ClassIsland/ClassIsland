using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClassIsland.Controls;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.ViewModels;
using MaterialDesignThemes.Wpf;
using unvell.ReoGrid;
using unvell.ReoGrid.Events;
using unvell.ReoGrid.Graphics;
using unvell.ReoGrid.IO;
using unvell.ReoGrid.IO.OpenXML.Schema;
using Path = System.IO.Path;

namespace ClassIsland.Views;

/// <summary>
/// ExcelImportWindow.xaml 的交互逻辑
/// </summary>
public partial class ExcelImportWindow : MyWindow
{
    public ExcelImportViewModel ViewModel { get; } = new();

    private ThemeService ThemeService { get; }

    private ProfileService ProfileService { get; }

    public string ExcelSourcePath { get; set; } = "";

    public static ICommand SelectionValueUpdateCommand { get; } = new RoutedUICommand();
    public static ICommand EnterSelectingModeCommand { get; } = new RoutedUICommand();

    public static ICommand NavigateCommand { get; } = new RoutedUICommand();

    public static ICommand NavigateBackCommand { get; } = new RoutedUICommand();

    public ExcelImportWindow(ThemeService themeService, ProfileService profileService)
    {
        InitializeComponent();
        DataContext = this;
        ThemeService = themeService;
        ProfileService = profileService;
        ThemeService.ThemeUpdated += ThemeServiceOnThemeUpdated;
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        
    }
    private void EnterSelectingMode()
    {
        BeginStoryboard((Storyboard)FindResource("EditingLoop"));
        ViewModel.IsSelectingMode = true;
        ViewModel.NormalSelectionRangePosition = Grid.CurrentWorksheet.SelectionRange;
        var s = typeof(ExcelImportViewModel).GetProperty(ViewModel.CurrentUpdatingPropertyName)?.GetValue(ViewModel);
        ViewModel.SelectedRangePosition = Grid.CurrentWorksheet.SelectionRange 
                = (RangePosition)(s ?? RangePosition.Empty);
        Grid.CurrentWorksheet.SelectionRangeChanged += CurrentWorksheetOnSelectionRangeChanged;
        if (ViewModel.CurrentSelectingElement != null) ViewModel.CurrentSelectingElement.IsSelecting = true;
        Grid.ControlStyle.SelectionBorderWidth = 5;
    }

    private void ExitSelectingMode()
    {
        ViewModel.IsSelectingMode = false;
        Grid.CurrentWorksheet.SelectionRangeChanged -= CurrentWorksheetOnSelectionRangeChanged;
        Grid.ControlStyle.SelectionBorderWidth = 3;
        Grid.CurrentWorksheet.SelectionRange = ViewModel.NormalSelectionRangePosition;
        if (ViewModel.CurrentSelectingElement != null) ViewModel.CurrentSelectingElement.IsSelecting = false;
        ViewModel.CurrentSelectingElement = null;
        ((Storyboard)FindResource("EditingLoop")).Stop();
        ((Storyboard)FindResource("EditingLoop")).Remove();
    }


    private void CurrentWorksheetOnSelectionRangeChanged(object? sender, RangeEventArgs e)
    {
        if (!ViewModel.IsSelectingMode)
        {
            return;
        }
        ViewModel.SelectedRangePosition = e.Range;
        var vmt = typeof(ExcelImportViewModel);
        var p = vmt.GetProperty(ViewModel.CurrentUpdatingPropertyName);
        if (p == null)
        {
            return; 
        }

        p.SetValue(ViewModel, e.Range);
        ViewModel.CurrentSelectingElement?.Focus();
        Debug.WriteLine(e.Range);
    }

    ~ExcelImportWindow()
    {
        ThemeService.ThemeUpdated -= ThemeServiceOnThemeUpdated;
    }

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
        rgcs[ControlAppearanceColors.GridBackground] = Color.FromArgb(255,255,255,255);
        rgcs[ControlAppearanceColors.GridText] = Color.FromArgb(255,0,0,0);
        rgcs[ControlAppearanceColors.GridLine] = Color.FromArgb(255,208,215,229);
        Grid.ControlStyle = rgcs;
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        UpdateChartStyle();
        if (File.Exists(ExcelSourcePath))
        {
            LoadExcelWorkbook();
        }
        base.OnContentRendered(e);
    }

    private async void LoadExcelWorkbook()
    {
        ViewModel.SlideIndex = 1;
        await Task.Run(() =>
        {
        });
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var stream = File.Open(ExcelSourcePath, FileMode.Open);

        var sw = new Stopwatch();
        sw.Start();
        Grid.Load(stream, FileFormat.Excel2007);
        Debug.WriteLine(sw.Elapsed);
        ViewModel.IsFileSelected = true;
        ViewModel.SlideIndex = 2;
    }

    private void ProfileSettingsWindow_OnDrop(object sender, DragEventArgs e)
    {
        if (ViewModel.IsFileSelected)
            return;
        if (e.Data.GetData(DataFormats.FileDrop) is not Array data)
            return;
        var filename = data.GetValue(0)?.ToString();
        if (filename == null)
            return;

        ExcelSourcePath = filename;
        LoadExcelWorkbook();
    }

    private void ProfileSettingsWindow_OnDragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) && !ViewModel.IsFileSelected 
            ? DragDropEffects.Link : DragDropEffects.None;
    }

    private void Grid_OnCurrentWorksheetChanged(object? sender, EventArgs e)
    {
        
    }

    private void Grid_OnBeforeActionPerform(object? sender, WorkbookActionEventArgs e)
    {
        Debug.WriteLine(e.Action.GetName());
        
    }

    private void SelectionValueUpdateCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        
    }

    private void EnterSelectingModeCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.CurrentUpdatingPropertyName = (string)e.Parameter;
        //ViewModel.CurrentSelectingElement = (ExcelSelectionTextBox)sender;
        EnterSelectingMode();
    }

    private void ButtonExitSelectingMode_OnClick(object sender, RoutedEventArgs e)
    {
        ExitSelectingMode();
    }

    private void NavigateCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not string p)
        {
            return;
        }

        ViewModel.SlideIndex = p switch
        {
            "TimeLayoutSource" => 4,
            "ImportTimeLayoutFromThisFile" => 5,
            "CreateTimeLayoutManually" => 6,
            "TimePointImportResult" => 7,
            "SelectSubjectsPosition" => 8,
            _ => ViewModel.SlideIndex
        };
        if (p == "ImportTimeLayoutFromThisFile" && ViewModel.SelectedTimeLayoutId == "")
        {
            ViewModel.SelectedTimeLayoutId = Guid.NewGuid().ToString();
            ProfileService.Profile.TimeLayouts.Add(ViewModel.SelectedTimeLayoutId, ViewModel.SelectedTimeLayout);
        }   
    }

    private void NavigateBackCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.SlideIndex = ViewModel.SlideIndex switch
        {
            3 => 2,
            4 => 3,
            5 => 4,
            6 => 4,
            7 => 5,
            8 when ViewModel.TimeLayoutImportSource == 0 => 7,
            8 when ViewModel.TimeLayoutImportSource == 2 => 6,
            _ => ViewModel.SlideIndex
        };
    }

    private TimeLayoutItem? ParseTimeLayoutItem(string? text)
    {
        var baseTime = DateTime.Now.Date;
        var baseDateOnly = new DateOnly(baseTime.Year, baseTime.Month, baseTime.Day);

        if (text == null) return null;
        var matches = Regex.Matches(text, "\\d+"); // 匹配数字
        if (matches.Count is not (4 or 6))  // 格式不符合
            return null;
        int h1 = 0, m1 = 0, s1 = 0, h2 = 0, m2 = 0, s2 = 0;
        switch (matches.Count)
        {
            // HH:MM - HH:MM
            case 4:
                h1 = int.Parse(matches[0].Value);
                m1 = int.Parse(matches[1].Value);
                h2 = int.Parse(matches[2].Value);
                m2 = int.Parse(matches[3].Value);
                break;
            //HH:MM:SS - HH:MM:SS
            case 6:
                h1 = int.Parse(matches[0].Value);
                m1 = int.Parse(matches[1].Value);
                s1 = int.Parse(matches[2].Value);
                h2 = int.Parse(matches[3].Value);
                m2 = int.Parse(matches[4].Value);
                s2 = int.Parse(matches[5].Value);
                break;
        }
        // 保存结果
        var result = new TimeLayoutItem()
        {
            StartSecond = new DateTime(baseDateOnly, new TimeOnly(h1, m1, s1)),
            EndSecond = new DateTime(baseDateOnly, new TimeOnly(h2, m2, s2))
        };
        return result;
    }

    private void LoadTimeLayoutFromCurrentSelection()
    {
        var baseTime = DateTime.Now.Date;
        var baseDateOnly = new DateOnly(baseTime.Year, baseTime.Month, baseTime.Day);

        var selection = ViewModel.TimePointSourcePosition;
        var timeLayout = ViewModel.SelectedTimeLayout;
        timeLayout.Layouts.Clear();
        // 判断是否是垂直模式
        if (selection.Rows != 1 && selection.Cols != 1)
        {
            return;
        }
        var isVertical = selection.Cols == 1;

        var start = isVertical? selection.Row : selection.Col;
        var end = isVertical? selection.EndRow : selection.EndCol;

        // 填充上课时间点
        for (var i = start; i <= end; i++)
        {
            var text = Grid.CurrentWorksheet.GetCellText(isVertical ? i : selection.Row, 
                isVertical? selection.Col : i);
            if (text == null) continue;
            var result = ParseTimeLayoutItem(text);
            if (result != null)
            {
                timeLayout.Layouts.Add(result);
            }
        }
        // 填充课间休息时间点
        var tempLayouts = (from i in timeLayout.Layouts select i).ToList();
        var pIndex = 0;
        for (int i = 0; i < tempLayouts.Count - 1; i++)
        {
            pIndex++;
            var a = tempLayouts[i];
            var b = tempLayouts[i + 1];
            if (b.StartSecond.TimeOfDay - a.EndSecond.TimeOfDay <= TimeSpan.Zero)
                continue;

            timeLayout.Layouts.Insert(pIndex, new TimeLayoutItem()
            {
                StartSecond = a.EndSecond,
                EndSecond = b.StartSecond,
                TimeType = 1
            });
            pIndex++;
        }
    }

    private void ButtonRefreshTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        LoadTimeLayoutFromCurrentSelection();
    }

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
}