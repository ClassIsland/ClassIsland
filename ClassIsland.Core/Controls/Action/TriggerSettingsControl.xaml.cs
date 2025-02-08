using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ClassIsland.Core.Models.Action;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.Core.Controls.Action;

/// <summary>
/// TriggerSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class TriggerSettingsControl : UserControl
{
    public static readonly DependencyProperty TriggersProperty = DependencyProperty.Register(
        nameof(Triggers), typeof(ObservableCollection<TriggerSettings>), typeof(TriggerSettingsControl), new PropertyMetadata(new ObservableCollection<TriggerSettings>()));

    public ObservableCollection<TriggerSettings> Triggers
    {
        get { return (ObservableCollection<TriggerSettings>)GetValue(TriggersProperty); }
        set { SetValue(TriggersProperty, value); }
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