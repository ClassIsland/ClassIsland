using ClassIsland.Models.AttachedSettings;
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
using ClassIsland.Interfaces;

namespace ClassIsland.Controls.AttachedSettingsControls;

/// <summary>
/// LessonControlAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class LessonControlAttachedSettingsControl : UserControl, IAttachedSettingsControlBase
{
    public LessonControlAttachedSettingsControl()
    {
        InitializeComponent();
    }

    public IAttachedSettingsHelper AttachedSettingsControlHelper { get; set; } =
        new AttachedSettingsControlHelper<LessonControlAttachedSettings>(
            new Guid("58e5b69a-764a-472b-bcf7-003b6a8c7fdf"), new LessonControlAttachedSettings());

    public LessonControlAttachedSettings Settings =>
        ((AttachedSettingsControlHelper<LessonControlAttachedSettings>)AttachedSettingsControlHelper)
        .AttachedSettings ?? new LessonControlAttachedSettings();
}