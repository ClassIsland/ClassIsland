using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Models.Weather;

namespace ClassIsland.Controls;

/// <summary>
/// WeatherRangePackIconControl.xaml 的交互逻辑
/// </summary>
public partial class WeatherRangePackIconControl : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(RangedValue), typeof(WeatherRangePackIconControl), new PropertyMetadata(default(RangedValue)));

    private bool _hasSecondIcon = false;

    public RangedValue? Value
    {
        get { return (RangedValue)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

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
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (Value != null)
        {
            HasSecondIcon = Value.From != Value.To;
        }
        base.OnPropertyChanged(e);
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