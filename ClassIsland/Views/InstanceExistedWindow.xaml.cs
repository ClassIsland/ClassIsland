using ClassIsland.Controls.Demo;
using ClassIsland.Core.Controls;
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

namespace ClassIsland.Views
{
    /// <summary>
    /// InstanceExistedWindow.xaml 的交互逻辑
    /// </summary>
    public partial class InstanceExistedWindow : MyWindow
    {
        public InstanceExistedWindow()
        {
            InitializeComponent();
        }

        private void ButtonRestart_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ButtonExit_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
