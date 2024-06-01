using System;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Services;

namespace ClassIsland.Controls;

/// <summary>
/// DebugTimeControl.xaml 的交互逻辑
/// </summary>
public partial class DebugTimeControl : UserControl
{
    private IExactTimeService ExactTimeService { get; } = App.GetService<IExactTimeService>();
    private SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    public static readonly DependencyProperty TargetDateTimeProperty = DependencyProperty.Register(
        nameof(TargetDateTime), typeof(DateTime), typeof(DebugTimeControl), new PropertyMetadata(default(DateTime)));

    public DateTime TargetDateTime
    {
        get { return (DateTime)GetValue(TargetDateTimeProperty); }
        set { SetValue(TargetDateTimeProperty, value); }
    }

    public DebugTimeControl()
    {
        InitializeComponent();
    }

    private void DebugTimeControl_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        TargetDateTime = ExactTimeService.GetCurrentLocalDateTime();
    }

    private void ButtonReset_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.TimeOffsetSeconds = 0;
    }

    private void ButtonConfirm_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.TimeOffsetSeconds = (TargetDateTime - DateTime.Now).TotalSeconds;
    }
}