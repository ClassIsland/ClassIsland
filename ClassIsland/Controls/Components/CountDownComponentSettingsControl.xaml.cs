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

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var selectedContent = selectedItem.Content.ToString();
            if (selectedContent == "自定义")
            {
                CustomMessageBox.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                CustomMessageBox.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
    }
}