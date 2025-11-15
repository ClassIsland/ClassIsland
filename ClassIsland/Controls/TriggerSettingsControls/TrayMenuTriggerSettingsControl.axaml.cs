using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Automation.Triggers;

namespace ClassIsland.Controls.TriggerSettingsControls;

public partial class TrayMenuTriggerSettingsControl : TriggerSettingsControlBase<TrayMenuTriggerSettings>
{
    public TrayMenuTriggerSettingsControl()
    {
        InitializeComponent();
    }
}