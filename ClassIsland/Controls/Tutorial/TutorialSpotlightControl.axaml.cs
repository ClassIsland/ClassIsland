using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace ClassIsland.Controls.Tutorial;

public partial class TutorialSpotlightControl : UserControl
{
    public static readonly StyledProperty<bool> FullscreenDimProperty = AvaloniaProperty.Register<TutorialSpotlightControl, bool>(
        nameof(FullscreenDim));

    public bool FullscreenDim
    {
        get => GetValue(FullscreenDimProperty);
        set => SetValue(FullscreenDimProperty, value);
    }

    public static readonly StyledProperty<bool> DisableIntroProperty = AvaloniaProperty.Register<TutorialSpotlightControl, bool>(
        nameof(DisableIntro));

    public bool DisableIntro
    {
        get => GetValue(DisableIntroProperty);
        set => SetValue(DisableIntroProperty, value);
    }

    public event EventHandler? Clicked;
    
    public TutorialSpotlightControl()
    {
        InitializeComponent();
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}