using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Models.Automation;
using CommunityToolkit.Mvvm.Input;
namespace ClassIsland.Core.Controls.Automation;

/// <summary>
/// TriggerSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class TriggerSettingsControl : UserControl
{
    public static readonly StyledProperty<ObservableCollection<TriggerSettings>> TriggersProperty = AvaloniaProperty.Register<TriggerSettingsControl, ObservableCollection<TriggerSettings>>(
        nameof(Triggers));

    public ObservableCollection<TriggerSettings> Triggers
    {
        get => GetValue(TriggersProperty);
        set => SetValue(TriggersProperty, value);
    }

    public TriggerSettingsControl()
    {
        InitializeComponent();
    }

    private void ButtonAddTrigger_OnClick(object sender, RoutedEventArgs e)
    {
        Triggers.Add(new TriggerSettings());
    }

    [RelayCommand]
    public void RemoveTrigger(TriggerSettings trigger)
    {
        Triggers.Remove(trigger);
    }
}
