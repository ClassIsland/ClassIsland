using System;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32;

namespace ClassIsland.Controls;

public class FileBrowserButton : Button
{
    public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(
        nameof(Filter), typeof(string), typeof(FileBrowserButton), new PropertyMetadata(default(string)));
    public string Filter
    {
        get
        {
            return (string)GetValue(FilterProperty);
        }
        set
        {
            SetValue(FilterProperty, value);
        }
    }

    public static readonly DependencyProperty CurrentPathProperty = DependencyProperty.Register(
        nameof(CurrentPath), typeof(string), typeof(FileBrowserButton), new PropertyMetadata(default(string)));
    public string CurrentPath
    {
        get
        {
            return (string)GetValue(CurrentPathProperty);
        }
        set
        {
            SetValue(CurrentPathProperty, value);
        }
    }

    public static readonly DependencyProperty StartFolderProperty = DependencyProperty.Register(
        nameof(StartFolder), typeof(string), typeof(FileBrowserButton), new PropertyMetadata(Environment.ProcessPath));

    public string StartFolder
    {
        get
        {
            return (string)GetValue(StartFolderProperty);
        }
        set
        {
            SetValue(StartFolderProperty, value);
        }
    }

    public event EventHandler? FileSelected; 


    static FileBrowserButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FileBrowserButton), new FrameworkPropertyMetadata(typeof(FileBrowserButton)));
    }

    protected override void OnClick()
    {
        base.OnClick();
        var dialog = new OpenFileDialog()
        {
            InitialDirectory = StartFolder,
            Filter = Filter,
            FileName = CurrentPath
        };
        if (dialog.ShowDialog() != true)
            return;
        CurrentPath = dialog.FileName;
        FileSelected?.Invoke(this, EventArgs.Empty);
    }
}