using System.Windows;
using System.Windows.Input;
using Avalonia;

namespace ClassIsland.Models;

public class WizardOption : AvaloniaObject
{
    public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<WizardOption, string>(
        nameof(Header));
    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    
    }

    public static readonly StyledProperty<string> ContentProperty = AvaloniaProperty.Register<WizardOption, string>(
        nameof(Content));
    public string Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    
    }

    public static readonly StyledProperty<ICommand> InvokeCommandProperty = AvaloniaProperty.Register<WizardOption, ICommand>(
        nameof(InvokeCommand));
    public ICommand InvokeCommand
    {
        get => GetValue(InvokeCommandProperty);
        set => SetValue(InvokeCommandProperty, value);
    
    }

    public static readonly StyledProperty<object> InvokeCommandParameterProperty = AvaloniaProperty.Register<WizardOption, object>(
        nameof(InvokeCommandParameter));
    public object InvokeCommandParameter
    {
        get => GetValue(InvokeCommandParameterProperty);
        set => SetValue(InvokeCommandParameterProperty, value);
    
    }
}