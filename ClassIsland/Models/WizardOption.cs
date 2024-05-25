using System.Windows;
using System.Windows.Input;

namespace ClassIsland.Models;

public class WizardOption : DependencyObject
{
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(WizardOption), new PropertyMetadata(default(string)));

    public string Header
    {
        get { return (string)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(string), typeof(WizardOption), new PropertyMetadata(default(string)));

    public string Content
    {
        get { return (string)GetValue(ContentProperty); }
        set { SetValue(ContentProperty, value); }
    }

    public static readonly DependencyProperty InvokeCommandProperty = DependencyProperty.Register(
        nameof(InvokeCommand), typeof(ICommand), typeof(WizardOption), new PropertyMetadata(default(ICommand)));

    public ICommand InvokeCommand
    {
        get { return (ICommand)GetValue(InvokeCommandProperty); }
        set { SetValue(InvokeCommandProperty, value); }
    }

    public static readonly DependencyProperty InvokeCommandParameterProperty = DependencyProperty.Register(
        nameof(InvokeCommandParameter), typeof(object), typeof(WizardOption), new PropertyMetadata(default(object)));

    public object InvokeCommandParameter
    {
        get { return (object)GetValue(InvokeCommandParameterProperty); }
        set { SetValue(InvokeCommandParameterProperty, value); }
    }
}