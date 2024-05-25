using System.Windows;

using MaterialDesignThemes.Wpf;

namespace ClassIsland.Models;

public class DialogAction : DependencyObject
{
    public static readonly DependencyProperty NameProperty = DependencyProperty.Register(
        nameof(Name), typeof(string), typeof(DialogAction), new PropertyMetadata(default(string)));

    public string Name
    {
        get { return (string)GetValue(NameProperty); }
        set { SetValue(NameProperty, value); }
    }

    public static readonly DependencyProperty UseCustomIconProperty = DependencyProperty.Register(
        nameof(UseCustomIcon), typeof(bool), typeof(DialogAction), new PropertyMetadata(default(bool)));

    public bool UseCustomIcon
    {
        get { return (bool)GetValue(UseCustomIconProperty); }
        set { SetValue(UseCustomIconProperty, value); }
    }

    public static readonly DependencyProperty PackIconKindProperty = DependencyProperty.Register(
        nameof(PackIconKind), typeof(PackIconKind), typeof(DialogAction), new PropertyMetadata(default(PackIconKind)));

    public PackIconKind PackIconKind
    {
        get { return (PackIconKind)GetValue(PackIconKindProperty); }
        set { SetValue(PackIconKindProperty, value); }
    }

    public static readonly DependencyProperty CustomIconProperty = DependencyProperty.Register(
        nameof(CustomIcon), typeof(object), typeof(DialogAction), new PropertyMetadata(default(object)));

    public object CustomIcon
    {
        get { return (object)GetValue(CustomIconProperty); }
        set { SetValue(CustomIconProperty, value); }
    }

    public static readonly DependencyProperty IsPrimaryProperty = DependencyProperty.Register(
        nameof(IsPrimary), typeof(bool), typeof(DialogAction), new PropertyMetadata(default(bool)));

    public bool IsPrimary
    {
        get { return (bool)GetValue(IsPrimaryProperty); }
        set { SetValue(IsPrimaryProperty, value); }
    }

}