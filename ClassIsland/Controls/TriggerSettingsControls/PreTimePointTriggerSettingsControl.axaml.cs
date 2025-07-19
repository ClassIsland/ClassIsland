using System;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Automation.Triggers;
using ClassIsland.Shared.Enums;

namespace ClassIsland.Controls.TriggerSettingsControls;

/// <summary>
/// PreTimePointTriggerSettingsControl.axaml 的交互逻辑
/// </summary>
public partial class PreTimePointTriggerSettingsControl : TriggerSettingsControlBase<PreTimePointTriggerSettings>
{
    public PreTimePointTriggerSettingsControl()
    {
        InitializeComponent();
        Loaded += PreTimePointTriggerSettingsControl_Initialized;
    }

    private void PreTimePointTriggerSettingsControl_Initialized(object? sender, EventArgs e)
    {
        StateComboBox.SelectedIndex = Settings.TargetState switch
        {
            TimeState.OnClass => 0,
            TimeState.Breaking => 1,
            TimeState.AfterSchool => 2,
            _ => -1,
        };
    }

    public void StateComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Settings.TargetState = StateComboBox.SelectedIndex switch
        {
            0 => TimeState.OnClass,
            1 => TimeState.Breaking,
            2 => TimeState.AfterSchool,
            _ => Settings.TargetState,
        };
    }
}