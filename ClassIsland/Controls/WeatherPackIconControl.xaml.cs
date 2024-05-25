using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

using ClassIsland.Services;

namespace ClassIsland.Controls;

/// <summary>
/// WeatherPackIconControl.xaml 的交互逻辑
/// </summary>
public partial class WeatherPackIconControl : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty CodeProperty = DependencyProperty.Register(
        nameof(Code), typeof(string), typeof(WeatherPackIconControl), new PropertyMetadata(default(string)));

    private string _weatherName = "";

    public string Code
    {
        get => (string)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public string WeatherName
    {
        get => _weatherName;
        set => SetField(ref _weatherName, value);
    }

    public WeatherPackIconControl()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == CodeProperty)
        {
            var c = (string)e.NewValue;
            WeatherName = App.GetService<WeatherService>().GetWeatherTextByCode(c);
        }
        base.OnPropertyChanged(e);
    }

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