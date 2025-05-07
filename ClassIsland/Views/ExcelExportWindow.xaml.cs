using ClassIsland.Core.Models.Theming;
using System;
using System.Collections.Generic;
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
using unvell.ReoGrid;
using unvell.ReoGrid.Events;
using unvell.ReoGrid.Graphics;
using RangePosition = unvell.ReoGrid.RangePosition;

namespace ClassIsland.Views;

/// <summary>
/// ExcelExportWindow.xaml 的交互逻辑
/// </summary>
public partial class ExcelExportWindow
{
    public IThemeService ThemeService { get; }

    public ExcelExportViewModel ViewModel { get; } = new();

    public ExcelExportWindow(IThemeService themeService)
    {
        ThemeService = themeService;
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
}