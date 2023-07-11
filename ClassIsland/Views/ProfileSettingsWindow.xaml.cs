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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.ViewModels;

namespace ClassIsland.Views;
/// <summary>
/// ProfileSettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class ProfileSettingsWindow : Window
{
    public MainViewModel ViewModel
    {
        get;
        set;
    } = new();

    public ProfileSettingsWindow()
    {
        InitializeComponent();
        DataContext = this;
    }
}
