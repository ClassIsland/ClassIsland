using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClassIsland.Core.Controls;

/// <summary>
/// WizardOptionControl.xaml 的交互逻辑
/// </summary>
public partial class WizardOptionControl : UserControl
{
    public static readonly DependencyProperty OptionHeaderProperty = DependencyProperty.Register(
        nameof(OptionHeader), typeof(string), typeof(WizardOptionControl), new PropertyMetadata(default(string)));

    public string OptionHeader
    {
        get { return (string)GetValue(OptionHeaderProperty); }
        set { SetValue(OptionHeaderProperty, value); }
    }

    public static readonly DependencyProperty OptionContentProperty = DependencyProperty.Register(
        nameof(OptionContent), typeof(string), typeof(WizardOptionControl), new PropertyMetadata(default(string)));

    public string OptionContent
    {
        get { return (string)GetValue(OptionContentProperty); }
        set { SetValue(OptionContentProperty, value); }
    }

    public static readonly DependencyProperty InvokedCommandProperty = DependencyProperty.Register(
        nameof(InvokedCommand), typeof(ICommand), typeof(WizardOptionControl), new PropertyMetadata(default(ICommand)));

    public ICommand InvokedCommand
    {
        get { return (ICommand)GetValue(InvokedCommandProperty); }
        set { SetValue(InvokedCommandProperty, value); }
    }

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected), typeof(bool), typeof(WizardOptionControl), new PropertyMetadata(default(bool)));

    public bool IsSelected
    {
        get { return (bool)GetValue(IsSelectedProperty); }
        set { SetValue(IsSelectedProperty, value); }
    }

    public static readonly DependencyProperty InvokeCommandParameterProperty = DependencyProperty.Register(
        nameof(InvokeCommandParameter), typeof(object), typeof(WizardOptionControl), new PropertyMetadata(default(object)));

    public object InvokeCommandParameter
    {
        get { return (object)GetValue(InvokeCommandParameterProperty); }
        set { SetValue(InvokeCommandParameterProperty, value); }
    }


    public WizardOptionControl()
    {
        InitializeComponent();
    }
}