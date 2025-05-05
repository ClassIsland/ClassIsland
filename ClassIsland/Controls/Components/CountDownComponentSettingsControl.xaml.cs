using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.ComponentSettings;
using System.Windows.Controls;

namespace ClassIsland.Controls.Components;

/// <summary>
/// CountDownComponentSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class CountDownComponentSettingsControl : ComponentBase<CountDownComponentSettings>
{
    private System.Windows.Controls.Control CustomMessageBox;

    public CountDownComponentSettingsControl()
    {
        InitializeComponent();
        CustomMessageBox = new System.Windows.Controls.Control(); // Initialize the control
    }
    
}