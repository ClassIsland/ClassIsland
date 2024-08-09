using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;

namespace ClassIsland.Controls;

public class AttachedSettingsControlPresenterCore : ContentPresenter
{
    public static readonly DependencyProperty ControlInfoProperty = DependencyProperty.Register(
        nameof(ControlInfo), typeof(AttachedSettingsControlInfo), typeof(AttachedSettingsControlPresenterCore), new PropertyMetadata(default(AttachedSettingsControlInfo)));

    public AttachedSettingsControlInfo? ControlInfo
    {
        get => (AttachedSettingsControlInfo?)GetValue(ControlInfoProperty);
        set => SetValue(ControlInfoProperty, value);
    }

    public static readonly DependencyProperty TargetObjectProperty = DependencyProperty.Register(
        nameof(TargetObject), typeof(AttachableSettingsObject), typeof(AttachedSettingsControlPresenterCore), new PropertyMetadata(default(AttachableSettingsObject)));

    public AttachableSettingsObject? TargetObject
    {
        get => (AttachableSettingsObject)GetValue(TargetObjectProperty);
        set => SetValue(TargetObjectProperty, value);
    }

    public static readonly DependencyProperty ContentObjectProperty = DependencyProperty.Register(
        nameof(ContentObject), typeof(object), typeof(AttachedSettingsControlPresenterCore), new PropertyMetadata(default(object)));

    public object? ContentObject
    {
        get => (object)GetValue(ContentObjectProperty);
        set => SetValue(ContentObjectProperty, value);
    }

    public static readonly DependencyProperty AssociatedAttachedSettingsProperty = DependencyProperty.Register(
        nameof(AssociatedAttachedSettings), typeof(IAttachedSettings), typeof(AttachedSettingsControlPresenterCore), new PropertyMetadata(default(IAttachedSettings?)));

    public IAttachedSettings? AssociatedAttachedSettings
    {
        get { return (IAttachedSettings?)GetValue(AssociatedAttachedSettingsProperty); }
        set { SetValue(AssociatedAttachedSettingsProperty, value); }
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == TargetObjectProperty)
        {
            UpdateContent();
        }
        base.OnPropertyChanged(e);
    }

    private void UpdateContent()
    {
        if (TargetObject == null || ControlInfo == null)
        {
            return;
        }

        TargetObject.AttachedObjects.TryGetValue(ControlInfo.Guid.ToLower(), out var settings);
        ContentObject = AttachedSettingsControlBase.GetInstance(ControlInfo, ref settings);
        //if (ContentObject is IAttachedSettingsControlBase c)
        //{
        //    c.AttachedSettingsControlHelper.AttachedTarget = TargetObject;
        //}
        Content = ContentObject;
        TargetObject.AttachedObjects[ControlInfo.Guid] = settings;
        AssociatedAttachedSettings = settings as IAttachedSettings;
    }
}