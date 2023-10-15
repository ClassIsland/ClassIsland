using ClassIsland.Interfaces;
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

namespace ClassIsland.Controls;

/// <summary>
/// AttachedSettingsControlPresenter.xaml 的交互逻辑
/// </summary>
public partial class AttachedSettingsControlPresenter : UserControl
{
    public static readonly DependencyProperty ControlInfoProperty = DependencyProperty.Register(
        nameof(ControlInfo), typeof(Type), typeof(AttachedSettingsControlPresenter), new PropertyMetadata(default(Type)));

    public Type? ControlInfo
    {
        get => (Type)GetValue(ControlInfoProperty);
        set => SetValue(ControlInfoProperty, value);
    }

    public static readonly DependencyProperty TargetObjectProperty = DependencyProperty.Register(
        nameof(TargetObject), typeof(AttachableSettingsObject), typeof(AttachedSettingsControlPresenter), new PropertyMetadata(default(AttachableSettingsObject)));

    public AttachableSettingsObject? TargetObject
    {
        get => (AttachableSettingsObject)GetValue(TargetObjectProperty);
        set => SetValue(TargetObjectProperty, value);
    }

    public static readonly DependencyProperty ContentObjectProperty = DependencyProperty.Register(
        nameof(ContentObject), typeof(object), typeof(AttachedSettingsControlPresenter), new PropertyMetadata(default(object)));

    public object? ContentObject
    {
        get => (object)GetValue(ContentObjectProperty);
        set => SetValue(ContentObjectProperty, value);
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

        ContentObject = Activator.CreateInstance(ControlInfo);
        if (ContentObject is IAttachedSettingsControlBase c)
        {
            c.AttachedSettingsControlHelper.AttachedTarget = TargetObject;
        }
        MainContentPresenter.Content = ContentObject;
    }

    public AttachedSettingsControlPresenter()
    {
        InitializeComponent();
    }
}