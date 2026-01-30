using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls.Ruleset;
using ClassIsland.Core.Models.Components;

namespace ClassIsland.Core.Controls;

public partial class ComponentAdvancedSettingsControl : UserControl
{
    public static readonly StyledProperty<ComponentSettings> SettingsProperty = AvaloniaProperty.Register<ComponentAdvancedSettingsControl, ComponentSettings>(
        nameof(Settings));

    public ComponentSettings Settings
    {
        get => GetValue(SettingsProperty);
        set => SetValue(SettingsProperty, value);
    }

    public static readonly StyledProperty<bool> IsRootComponentProperty = AvaloniaProperty.Register<ComponentAdvancedSettingsControl, bool>(
        nameof(IsRootComponent));

    public bool IsRootComponent
    {
        get => GetValue(IsRootComponentProperty);
        set => SetValue(IsRootComponentProperty, value);
    }
    
    public ComponentAdvancedSettingsControl()
    {
        InitializeComponent();
    }
    
    public static readonly RoutedEvent<RoutedEventArgs> RequestOpenHideRulesEvent =
        RoutedEvent.Register<RoutedEventArgs>(
            nameof(RequestOpenHideRules), RoutingStrategies.Bubble, typeof(ComponentAdvancedSettingsControl));
     
    public event EventHandler<RoutedEventArgs> RequestOpenHideRules
    {
        add => AddHandler(RequestOpenHideRulesEvent, value);
        remove => RemoveHandler(RequestOpenHideRulesEvent, value);
    }
    
    private void ButtonOpenRuleset_OnClick(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(RequestOpenHideRulesEvent));
    }
}