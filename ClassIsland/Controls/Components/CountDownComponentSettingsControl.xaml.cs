using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.ComponentSettings;

namespace ClassIsland.Controls.Components;

/// <summary>
/// CountDownComponentSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class CountDownComponentSettingsControl : ComponentBase<CountDownComponentSettings>
{
    public CountDownComponentSettingsControl()
    {
        InitializeComponent();
        this.Settings.PropertyChanged += Settings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.IsConnectorColorEmphasized) || e.PropertyName == nameof(Settings.FontColor))
        {
            Settings.UpdateConnectorColor();
        }
    }
}