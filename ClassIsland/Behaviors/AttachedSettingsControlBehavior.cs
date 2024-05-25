using System;
using System.Windows;
using System.Windows.Controls;

using ClassIsland.Core;

using Microsoft.Xaml.Behaviors;

namespace ClassIsland.Behaviors;

public class AttachedSettingsControlBehavior : Behavior<ContentPresenter>
{
    public static readonly DependencyProperty AttachTargetObjectProperty = DependencyProperty.Register(
        nameof(AttachTargetObject), typeof(AttachableSettingsObject), typeof(AttachedSettingsControlBehavior), new PropertyMetadata(default(AttachableSettingsObject)));

    public AttachableSettingsObject AttachTargetObject
    {
        get => (AttachableSettingsObject)GetValue(AttachTargetObjectProperty);
        set => SetValue(AttachTargetObjectProperty, value);
    }

    public static readonly DependencyProperty AttachedSettingControlTypeProperty = DependencyProperty.Register(
        nameof(AttachedSettingControlType), typeof(Type), typeof(AttachedSettingsControlBehavior), new PropertyMetadata(default(Type)));

    public Type? AttachedSettingControlType
    {
        get => (Type)GetValue(AttachedSettingControlTypeProperty);
        set => SetValue(AttachedSettingControlTypeProperty, value);
    }
    
    protected override void OnAttached()
    {
        UpdateAttached();

        base.OnAttached();
    }

    private void UpdateAttached() 
    {
        if (AssociatedObject == null || AttachedSettingControlType == null)
        {
            return;
        }

        AssociatedObject.Content = Activator.CreateInstance(AttachedSettingControlType);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == AttachTargetObjectProperty)
        {
            UpdateAttached();
        }
        base.OnPropertyChanged(e);
    }
}