using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;

namespace ClassIsland.Controls;

/// <summary>
/// WeatherPackIconControl.xaml 的交互逻辑
/// </summary>
[PseudoClasses(":rainy")]
public partial class WeatherPackIconControl : UserControl, INotifyPropertyChanged
{
    private string _weatherName = "";
    
    public static readonly StyledProperty<string> CodeProperty = AvaloniaProperty.Register<WeatherPackIconControl, string>(
        nameof(Code));

    public string Code
    {
        get => GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public static readonly StyledProperty<string> WeatherNameProperty = AvaloniaProperty.Register<WeatherPackIconControl, string>(
        nameof(WeatherName));

    public string WeatherName
    {
        get => GetValue(WeatherNameProperty);
        set => SetValue(WeatherNameProperty, value);
    }

    public static readonly StyledProperty<bool> AllowColorProperty = AvaloniaProperty.Register<WeatherPackIconControl, bool>(
        nameof(AllowColor), true);

    public bool AllowColor
    {
        get => GetValue(AllowColorProperty);
        set => SetValue(AllowColorProperty, value);
    }

    private string _weatherColor = "";

    public string WeatherColor
    {
        get => _weatherColor;
        set => SetField(ref _weatherColor, value);
    }

    public IWeatherService? WeatherService => IAppHost.TryGetService<IWeatherService>();

    public WeatherPackIconControl()
    {
        InitializeComponent();
        this.GetObservable(CodeProperty).Subscribe(_ =>
        {
            WeatherName = App.GetService<IWeatherService>().GetWeatherTextByCode(Code);
            if (AllowColor)
            {
                PseudoClasses.Set(":rainy", (WeatherName.Contains('雨') || WeatherName is "飑"));
            }
            else
            {
                PseudoClasses.Set(":rainy", false);
            }
        });
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