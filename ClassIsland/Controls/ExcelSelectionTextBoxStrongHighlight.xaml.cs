using System;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ClassIsland.Controls;

/// <summary>
/// ExcelSelectionTextBoxStrongHighlight.xaml 的交互逻辑
/// </summary>
public partial class ExcelSelectionTextBoxStrongHighlight : UserControl
{
    public ExcelSelectionTextBoxStrongHighlight()
    {
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        BeginStoryboard((Storyboard)FindResource("Loop"));
        base.OnInitialized(e);
    }
}