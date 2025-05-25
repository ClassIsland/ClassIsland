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
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Action;

namespace ClassIsland.Core.Controls.Action;

/// <summary>
/// TriggerSettingsControlPresenter.xaml 的交互逻辑
/// </summary>
public partial class TriggerSettingsControlPresenter : UserControl
{
    public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
        nameof(Settings), typeof(TriggerSettings), typeof(TriggerSettingsControlPresenter), new PropertyMetadata(default(TriggerSettings),
            (o, args) =>
            {
                if (o is TriggerSettingsControlPresenter control)
                {
                    control.UpdateContent();
                }
            }));

    public TriggerSettings? Settings
    {
        get { return (TriggerSettings)GetValue(SettingsProperty); }
        set { SetValue(SettingsProperty, value); }
    }

    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(
        nameof(Id), typeof(string), typeof(TriggerSettingsControlPresenter), new PropertyMetadata("", (o, args) =>
        {
            if (o is TriggerSettingsControlPresenter control)
            {
                control.UpdateContent();
            }
        }));

    public string? Id
    {
        get { return (string)GetValue(IdProperty); }
        set { SetValue(IdProperty, value); }
    }

    public TriggerSettingsControlPresenter()
    {
        InitializeComponent();
    }

    private void UpdateContent()
    {
        if (Settings is not { AssociatedTriggerInfo: not null })
        {
            return;
        }
        

        var settings = Settings.Settings;
        RootContentPresenter.Content = TriggerSettingsControlBase.GetInstance(Settings.AssociatedTriggerInfo, ref settings);
        Settings.Settings = settings;
    }
}