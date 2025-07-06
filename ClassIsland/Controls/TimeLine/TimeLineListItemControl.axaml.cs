using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.TimeLine;

/// <summary>
/// TimeLineListItemControl.xaml 的交互逻辑
/// </summary>
public sealed partial class TimeLineListItemControl : UserControl, INotifyPropertyChanged
{
    public static readonly StyledProperty<TimeLayoutItem?> TimePointProperty = AvaloniaProperty.Register<TimeLineListItemControl, TimeLayoutItem?>(
        nameof(TimePoint));

    public TimeLayoutItem? TimePoint
    {
        get => GetValue(TimePointProperty);
        set => SetValue(TimePointProperty, value);
    }

    public static readonly StyledProperty<bool> IsAlwaysExpandedProperty = AvaloniaProperty.Register<TimeLineListItemControl, bool>(
        nameof(IsAlwaysExpanded));

    public bool IsAlwaysExpanded
    {
        get => GetValue(IsAlwaysExpandedProperty);
        set => SetValue(IsAlwaysExpandedProperty, value);
    }

    private bool _isExpanded = true;

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
        IsExpanded = IsAlwaysExpanded || Bounds.Height >= 16;
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