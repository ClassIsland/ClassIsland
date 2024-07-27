using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using ClassIsland.Controls;
using ClassIsland.Converters;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Converters;
using ClassIsland.Core.Models.Theming;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.ViewModels;

using MaterialDesignThemes.Wpf;

using Microsoft.AppCenter.Analytics;

using OfficeOpenXml;

using unvell.ReoGrid;
using unvell.ReoGrid.Events;
using unvell.ReoGrid.Graphics;
using unvell.ReoGrid.IO;

using CheckBox = System.Windows.Controls.CheckBox;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using Sentry;

namespace ClassIsland.Views;

/// <summary>
/// ExcelImportWindow.xaml 的交互逻辑
/// </summary>
public partial class ExcelImportWindow : MyWindow
{
    public ExcelImportViewModel ViewModel { get; } = new();

    private IThemeService ThemeService { get; }

    public IProfileService ProfileService { get; }

    public string ExcelSourcePath { get; set; } = "";

    public static ICommand SelectionValueUpdateCommand { get; } = new RoutedUICommand();
    public static ICommand EnterSelectingModeCommand { get; } = new RoutedUICommand();

    public static ICommand NavigateCommand { get; } = new RoutedUICommand();

    public static ICommand NavigateBackCommand { get; } = new RoutedUICommand();

    public bool ImportTimeLayoutOnly { get; set; } = false;

    public ExcelImportWindow(IThemeService themeService, IProfileService profileService)
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

    private async Task<object?> ShowDialog(string key)
    {
        return await DialogHost.Show(FindResource(key), ViewModel.DialogId);
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

        Grid.SheetTabWidth = 400;
        base.OnContentRendered(e);
    }

    private async Task FixWorkbookAsync(string path)
    {
        using var excel = new ExcelPackage(path);
        await Task.Run(() =>
        {
            //foreach (var i in excel.Workbook.Styles.Fonts)
            //{
            //    i.Name = "Microsoft YaHei";
            //}
            excel.Workbook.Styles.UpdateXml();
            foreach (var sheet in excel.Workbook.Worksheets)
            {
                foreach (var cell in sheet.Cells)
                {
                    if (!cell.IsRichText) continue;
                    var text = cell.RichText.Text;
                    Console.WriteLine(cell.RichText.HtmlText);
                    cell.RichText.Clear();
                    cell.Value = text;
                    //cell.RichText.Text = text;
                }
            }
        });

        var filePath = $"./Temp/excel{Guid.NewGuid()}.xlsx";
        await excel.SaveAsAsync(filePath);
        ExcelSourcePath = filePath ;
    }

    private async void LoadExcelWorkbook(bool isFixed=false)
    {
        ViewModel.SlideIndex = 1;
        ViewModel.OpenFileName = ExcelSourcePath;
        await Dispatcher.Yield();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            var stream = File.Open(ExcelSourcePath, FileMode.Open);
            var sw = new Stopwatch();
            sw.Start();
            App.GetService<IHangService>().AssumeHang();
            try
            {
                Grid.Load(stream, FileFormat.Excel2007);
            }
            catch (Exception e)
            {
                ViewModel.OpenException = e;
                stream.Close();
                if (isFixed)
                {
                    await ShowDialog("FixExcelFailed");
                    return;
                }
                var r = await ShowDialog("ExcelOpenError") as bool?;
                if (r != true || isFixed)
                {
                    ViewModel.SlideIndex = 0;
                    return;
                }
                await FixWorkbookAsync(ExcelSourcePath);
                LoadExcelWorkbook(true);
                return;
            }
            Debug.WriteLine(sw.Elapsed);
            ViewModel.IsFileSelected = true;
            ViewModel.SlideIndex = 2;
        }
        catch (Exception e)
        {
            ViewModel.SlideIndex = 0;
            ViewModel.OpenException = e;
            await ShowDialog("OpenFileFailed");
        }

    }

    private void ProfileSettingsWindow_OnDrop(object sender, DragEventArgs e)
    {
        ViewModel.IsDragEntering = false;
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
        ViewModel.IsDragEntering = e.Data.GetDataPresent(DataFormats.FileDrop) && !ViewModel.IsFileSelected;
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

    private void OpenProfileSettingsWindow()
    {
        var window = App.GetService<ProfileSettingsWindow>();
        window.Open();
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

        if (ViewModel.SlideIndex == 6)
        {
            ViewModel.ClassRecognitionTimePoints.Clear();
            ViewModel.SelectedTimeLayout = ProfileService.Profile.TimeLayouts[ViewModel.SelectedTimeLayoutId];
            foreach (var i in ViewModel.SelectedTimeLayout.Layouts)
            {
                if (i.TimeType == 0)
                {
                    ViewModel.ClassRecognitionTimePoints.Add(new Selectable<TimeLayoutItem>(i));
                }
            }
        }

        ViewModel.SlideIndex = p switch
        {
            //"TimeLayoutSource" when ImportTimeLayoutOnly => 5,
            "TimeLayoutSource" => 4,
            "ImportTimeLayoutFromThisFile" => 5,
            "CreateTimeLayoutManually" => 6,
            "ImportTimeLayoutFromOtherFile" => 6,
            "TimePointImportResult" => 7,
            "SelectSubjectsPosition" when ImportTimeLayoutOnly => 16,
            "SelectSubjectsPosition" => 8,
            "SelectVerticalMode" => 9,
            "RowClassesTimeRelationshipImportMethod" => 10,
            "RowClassesTimeRelationshipImportAuto" => 11,
            "RowClassesTimeRelationshipImportMan" => 11,
            "SelectClassPlanArea" => 12,
            "PreviewClassPlan" => 13,
            "ClassPlanDetails" => 14,
            "AllClassPlansView" => 15,
            "Finish" => 16,
            _ => ViewModel.SlideIndex
        };
        switch (p)
        {
            case "ImportTimeLayoutFromThisFile" when ViewModel.SelectedTimeLayoutId == "" && ViewModel.TimeLayoutImportSource != 2:
                ViewModel.SelectedTimeLayoutId = Guid.NewGuid().ToString();
                ProfileService.Profile.TimeLayouts.Add(ViewModel.SelectedTimeLayoutId, ViewModel.SelectedTimeLayout);
                break;
            case "TimePointImportResult":
                LoadTimeLayoutFromCurrentSelection();
                break;
            case "RowClassesTimeRelationshipImportAuto":
            case "RowClassesTimeRelationshipImportMan":
                ViewModel.SelectedTimeLayout.IsActivatedManually = true;
                ViewModel.SelectedTimeLayout.IsActivated = true;
                LoadClassPlanSource();
                break;
            case "PreviewClassPlan":
                LoadClassPlan();
                break;
            case "AllClassPlansView":
                ViewModel.ImportedClassPlans.Add(ViewModel.CurrentClassPlan);
                break;
            case "ImportTimeLayoutFromOtherFile":
                var w = App.GetService<ExcelImportWindow>();
                w.ImportTimeLayoutOnly = true;
                w.ShowDialog();
                if (!string.IsNullOrEmpty(w.ViewModel.SelectedTimeLayoutId))
                {
                    ViewModel.SelectedTimeLayoutId = w.ViewModel.SelectedTimeLayoutId;
                }
                break;
        }
    }

    private void NavigateBackCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.SlideIndex = ViewModel.SlideIndex switch
        {
            3 => 2,
            4 => 2,
            5 => 4,
            6 => 4,
            7 => 5,
            8 when ViewModel.TimeLayoutImportSource == 0 => 7,
            8 when ViewModel.TimeLayoutImportSource == 2 => 9,
            9 => 6,
            10 => 8,
            11 => 10,
            12 => 11,
            13 => 12,
            14 => 13,
            15 => 14,
            16 => 15,
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
        var auto = ViewModel.TimeLayoutImportSource != 2;
        if (auto)
        {
            timeLayout.Layouts.Clear();
        }
        ViewModel.ClassRecognitionRange.Clear();
        ViewModel.ClassRecognitionRangeCache.Clear();
        // 判断是否是垂直模式
        if (selection.Rows != 1 && selection.Cols != 1)
        {
            return;
        }
        var isVertical = selection.Cols == 1;
        ViewModel.IsVerticalLayout = isVertical;

        var start = isVertical? selection.Row : selection.Col;
        var end = isVertical? selection.EndRow : selection.EndCol;

        // 填充上课时间点
        for (var i = start; i <= end; i++)
        {
            var text = Grid.CurrentWorksheet.GetCellText(isVertical ? i : selection.Row, 
                isVertical? selection.Col : i);
            if (text == null) 
                continue;
            var result = ParseTimeLayoutItem(text);
            if (result == null) 
                continue;
            if (auto)
            {
                timeLayout.Layouts.Add(result);
            }
            ViewModel.ClassRecognitionRangeCache.Add(i);
        }

        // 填充课间休息时间点
        if (!auto)
            return;
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

        if (timeLayout.Layouts.Count > 0)
            TimeLineListControl.ScrollIntoView(timeLayout.Layouts[0]);
    }

    private void LoadClassPlanSource()
    {
        ViewModel.SelectedTimeLayout = ProfileService.Profile.TimeLayouts[ViewModel.SelectedTimeLayoutId];
        var isVertical = ViewModel.IsVerticalLayout;
        var selection = ViewModel.SubjectSourcePosition;

        // 填充课表源
        var start = isVertical ? selection.Col : selection.Row;
        var end = isVertical ? selection.EndCol : selection.EndRow;
        ViewModel.ClassPlanSources.Clear();
        for (var i = start; i <= end; i++)
        {
            ViewModel.ClassPlanSources.Add(isVertical
                ? new RangePosition(selection.Row, i, selection.Rows, 1)
                : new RangePosition(i, selection.Col, 1, selection.Cols));
        }

        // 填充时间识别源
        var startR = isVertical ?  selection.Row : selection.Col;
        var endR = isVertical ? selection.EndRow : selection.EndCol;
        var auto = ViewModel.TimeLayoutImportSource != 2;
        ViewModel.ClassRecognitionRange.Clear();
        if (auto)
        {
            ViewModel.ClassRecognitionTimePoints.Clear();
        }
        for (var i = startR; i <= endR; i++)
        {
            var range = isVertical
                ? new RangePosition(i, selection.Col, 1, selection.EndCol)
                : new RangePosition(selection.Row, i, selection.EndRow, 1);
            var selectableRange = new Selectable<RangePosition>(range);
            ViewModel.ClassRecognitionRange.Add(selectableRange);
            if (ViewModel.ClassRecognitionRangeCache.Contains(i))
            {
                selectableRange.IsSelected = true;
            }

            var posRow = isVertical ? i : ViewModel.TimePointSourcePosition.Row;
            var posCol = isVertical ? ViewModel.TimePointSourcePosition.Col : i;
            var text = Grid.CurrentWorksheet.GetCellText(posRow, posCol);
            var result = ParseTimeLayoutItem(text);
            if (result == null) 
                continue;
            // 匹配时间点
            if (auto)
            {
                var selectableTimePoint = new Selectable<TimeLayoutItem>(result);
                ViewModel.ClassRecognitionTimePoints.Add(selectableTimePoint);
                var matched = ViewModel.SelectedTimeLayout.Layouts.Where(k => k.TimeType == 0).Any(k =>
                    k.StartSecond.TimeOfDay == result.StartSecond.TimeOfDay);
                if (matched)
                {
                    selectableTimePoint.IsSelected = true;
                }
            }
        }
    }

    private void LoadClassPlan()
    {
        ViewModel.CurrentClassPlan = new ClassPlan();
        var source = ViewModel.CurrentClassPlanSource;
        if (source == RangePosition.Empty)
            return;
        var isVertical = ViewModel.IsVerticalLayout;
        
        int gi = 0, ti = 0, ci = 0; // index of ViewModel.ClassRecognitionRange, index of ViewModel.ClassRecognitionTimePoints, 当前下标
        var start = isVertical ? source.Row : source.Col;
        var end = isVertical ? source.EndRow : source.EndCol;
        var subjects = ProfileService.Profile.Subjects;
        var timeLayout = ViewModel.SelectedTimeLayout;
        var classPlan = ViewModel.CurrentClassPlan;

        // 初始化当前课表
        classPlan.Classes.Clear();
        classPlan.TimeLayouts = ProfileService.Profile.TimeLayouts;
        classPlan.TimeLayoutId = ViewModel.SelectedTimeLayoutId;
        classPlan.RefreshClassesList();
        var d = (SubjectsDictionaryValueAccessConverter)FindResource("DictionaryValueAccessConverter");
        d.SourceDictionary = ProfileService.Profile.Subjects;
        var count = (from j in timeLayout.Layouts where j.TimeType == 0 select j).ToList().Count;
        //for (var i = 0; i < count; i++)
        //{
        //    classPlan.Classes.Add(new ClassInfo());
        //}

        foreach (var i in ViewModel.ClassRecognitionRange)
        {
            if (!i.IsSelected)
                continue;
            // 移动下标
            while (ti < ViewModel.ClassRecognitionTimePoints.Count && 
                   !ViewModel.ClassRecognitionTimePoints[ti].IsSelected) 
                ti++;
            var v = i.Value;
            var row = isVertical? v.Row : source.Row;
            var col = isVertical? source.Col : v.Col;
            var text = Grid.CurrentWorksheet.GetCellText(row, col);
            if (ti >= classPlan.Classes.Count)
                break;
            // 搜索文本内容
            foreach (var s in subjects.Where(s => text.Contains(s.Value.Name)))
            {
                classPlan.Classes[ti].SubjectId = s.Key;
                break;
            }

            ti++;
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

    private void SelectorClassPlanSource_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count <= 0)
            return;
        var r = (RangePosition?)e.AddedItems[0];
        if (r != null)
        {
            Grid.CurrentWorksheet.SelectionRange = r.Value;
        }
    }

    private void MenuItemContinueImportClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SlideIndex = 12;
    }

    private void CompleteImport()
    {
        foreach (var i in ViewModel.ImportedClassPlans)
        {
            ProfileService.Profile.ClassPlans.Add(Guid.NewGuid().ToString(), i);
        }

        ViewModel.SelectedTimeLayout.IsActivated = false;
        ViewModel.SelectedTimeLayout.IsActivatedManually = false;
    }

    private void MenuItemCompleteImport_OnClick(object sender, RoutedEventArgs e)
    {
        CompleteImport();
        ViewModel.SlideIndex = 16;
    }

    private void ButtonFinished_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RangeSelection_OnMouseEnter(object sender, MouseEventArgs e)
    {
        var s = sender as CheckBox;
        Debug.WriteLine(s?.Content);
        if (s?.Content != null)
        {
            Grid.CurrentWorksheet.SelectRange((RangePosition)s.Content);
        }
    }

    private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled)
        {
            // ListView拦截鼠标滚轮事件
            e.Handled = true;

            // 激发一个鼠标滚轮事件，冒泡给外层ListView接收到
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            var parent = ((System.Windows.Controls.Control)sender).Parent as UIElement;
            if (parent != null)
            {
                parent.RaiseEvent(eventArg);
            }
        }
    }

    private void ButtonBrowseExcelFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要导入的表格文件",
            InitialDirectory = Environment.SpecialFolder.Desktop.ToString(),
            Filter = "表格文件(*.xlsx)|*.xlsx"
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) 
        {
            ExcelSourcePath = dialog.FileName;
            LoadExcelWorkbook();
        }
    }

    private void TextBoxTimeLayoutRange_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        // 验证时间表区域是否合法
        var range = ViewModel.TimePointSourcePosition;
        if (range.IsEmpty)
        {
            ViewModel.IsTimeLayoutRangeValid = false;
            return;
        }

        if (range.Rows == 1 || range.Cols == 1)
        {
            ViewModel.IsTimeLayoutRangeValid = true;
            return;
        }
        ViewModel.IsTimeLayoutRangeValid = false;
    }

    private void ButtonOpenProfileSettingsWindow_OnClick(object sender, RoutedEventArgs e)
    {
        var p = App.GetService<ProfileSettingsWindow>();
        OpenProfileSettingsWindow();
        p.OpenTimeLayoutEdit();
    }

    private void TextBoxClassPlanRange_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var range = ViewModel.SubjectSourcePosition;
        ViewModel.IsSubjectsSourceRangeValid = !range.IsEmpty;
    }

    private void MenuItemResetTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedTimeLayout.IsActivated = false;
        ViewModel.SelectedTimeLayout.IsActivatedManually = false;
        ViewModel.SelectedTimeLayoutId = "";
        ViewModel.SelectedTimeLayout = new TimeLayout();
        ViewModel.SlideIndex = 4;
    }

    private void ExcelImportWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        ViewModel.SelectedTimeLayout.IsActivated = false;
        ViewModel.SelectedTimeLayout.IsActivatedManually = false;
    }

    private void ButtonOpenProfileSettingsWindowEdit_OnClick(object sender, RoutedEventArgs e)
    {
        OpenProfileSettingsWindow();
        App.GetService<ProfileSettingsWindow>().OpenTimeLayoutEdit(ViewModel.SelectedTimeLayoutId);
    }

    private void ExcelImportWindow_OnDragLeave(object sender, DragEventArgs e)
    {
        ViewModel.IsDragEntering = false;
    }

    private void ButtonSkipImport_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SlideIndex = 16;
        CompleteImport();
    }
}