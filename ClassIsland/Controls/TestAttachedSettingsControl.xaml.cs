using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using ClassIsland.Interfaces;
using ClassIsland.Models.AttachedSettings;

namespace ClassIsland.Controls;

/// <summary>
/// TestAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class TestAttachedSettingsControl : UserControl, IAttachedSettingsControlBase
{
    public TestAttachedSettingsControl()
    {
        InitializeComponent();
    }

    public AttachableSettingsObject? AttachedTarget { get; set; }

    public IAttachedSettingsHelper AttachedSettingsControlHelper { get; set; } =
        new AttachedSettingsControlHelper<TestAttachedSettings>(new Guid("6A692B17-4C37-4378-98EC-42730FB28C7C"),
            new TestAttachedSettings());


    public TestAttachedSettings? Settings =>
        (TestAttachedSettings?)((AttachedSettingsControlHelper<TestAttachedSettings>)AttachedSettingsControlHelper)
        .AttachedSettings;

}