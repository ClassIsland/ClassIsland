using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

    public string ExcelSourcePath { get; set; } = "";

    public static ICommand SelectionValueUpdateCommand { get; } = new RoutedUICommand();
    public static ICommand EnterSelectingModeCommand { get; } = new RoutedUICommand();

    public ExcelImportWindow(ThemeService themeService)
    {
        InitializeComponent();
        DataContext = this;
        ThemeService = themeService;
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
        Grid.CurrentWorksheet.SelectionRange = (RangePosition)(s ?? RangePosition.Empty);
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
}