using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using ClassIsland.Core.Models.Weather;

namespace ClassIsland.Controls;

/// <summary>
/// WeatherRangePackIconControl.xaml 的交互逻辑
/// </summary>
public partial class WeatherRangePackIconControl : UserControl, INotifyPropertyChanged
{
    public static readonly StyledProperty<RangedValue?> ValueProperty = AvaloniaProperty.Register<WeatherRangePackIconControl, RangedValue?>(
        nameof(Value));

    public RangedValue? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    

    private bool _hasSecondIcon = false;


    public bool HasSecondIcon
    {
        get => _hasSecondIcon;
        set
        {
            if (value == _hasSecondIcon) return;
            _hasSecondIcon = value;
            OnPropertyChanged();
        }
    }

    public WeatherRangePackIconControl()
    {
        InitializeComponent();
        this.GetObservable(ValueProperty).Subscribe(_ => HasSecondIcon = Value != null && Value.From != Value.To);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
