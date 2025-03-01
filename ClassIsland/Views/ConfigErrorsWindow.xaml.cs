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
using ClassIsland.Shared.Models;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.Views;

/// <summary>
/// ConfigErrorsWindow.xaml 的交互逻辑
/// </summary>
public partial class ConfigErrorsWindow
{
    public ConfigErrorsWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    [RelayCommand]
    private void CopyErrorDetails(ConfigError error)
    {
        Clipboard.SetDataObject(error.Exception.ToString(), false);
    }
}