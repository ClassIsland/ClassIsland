using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using ClassIsland.Models.Weather;

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