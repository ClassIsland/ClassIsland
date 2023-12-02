using System;
using System.Collections.Generic;
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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClassIsland.Controls;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.ViewModels;
using unvell.ReoGrid;
using unvell.ReoGrid.Graphics;
using unvell.ReoGrid.IO;
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

    public ExcelImportWindow(ThemeService themeService)
    {
        InitializeComponent();
        DataContext = this;
        ThemeService = themeService;
        ThemeService.ThemeUpdated += ThemeServiceOnThemeUpdated;
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
        rgcs.SelectionBorderWidth = 3;
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
}