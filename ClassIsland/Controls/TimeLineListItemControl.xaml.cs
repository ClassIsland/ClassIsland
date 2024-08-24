using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls;

/// <summary>
/// TimeLineListItemControl.xaml 的交互逻辑
/// </summary>
public sealed partial class TimeLineListItemControl : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty TimePointProperty = DependencyProperty.Register(
        nameof(TimePoint), typeof(TimeLayoutItem), typeof(TimeLineListItemControl), new PropertyMetadata(default(TimeLayoutItem)));

    public static readonly DependencyProperty IsAlwaysExpandedProperty = DependencyProperty.Register(
        nameof(IsAlwaysExpanded), typeof(bool), typeof(TimeLineListItemControl), new PropertyMetadata(false));

    public bool IsAlwaysExpanded
    {
        get { return (bool)GetValue(IsAlwaysExpandedProperty); }
        set { SetValue(IsAlwaysExpandedProperty, value); }
    }

    private bool _isExpanded = true;

    public TimeLayoutItem TimePoint
    {
        get { return (TimeLayoutItem)GetValue(TimePointProperty); }
        set { SetValue(TimePointProperty, value); }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (value == _isExpanded) return;
            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public TimeLineListItemControl()
    {
        InitializeComponent();
    }

    private void CheckSize()
    {
        IsExpanded = IsAlwaysExpanded || ActualHeight >= 16;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void TimeLineListItemControl_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        CheckSize();
    }
}