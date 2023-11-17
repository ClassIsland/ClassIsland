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
using ClassIsland.Controls;
using ClassIsland.ViewModels;

namespace ClassIsland.Views;

/// <summary>
/// ExcelImportWindow.xaml 的交互逻辑
/// </summary>
public partial class ExcelImportWindow : MyWindow
{
    public ExcelImportViewModel ViewModel { get; } = new();

    public string ExcelSourcePath { get; set; } = "";

    public ExcelImportWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        
        base.OnContentRendered(e);
    }
}