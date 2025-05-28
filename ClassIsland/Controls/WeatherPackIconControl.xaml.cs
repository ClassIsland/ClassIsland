﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;

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

    public static readonly DependencyProperty AllowColorProperty = DependencyProperty.Register(
        nameof(AllowColor), typeof(bool), typeof(WeatherPackIconControl), new PropertyMetadata(true));

    public bool AllowColor
    {
        get => (bool)GetValue(AllowColorProperty);
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
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == CodeProperty)
        {
            var c = (string)e.NewValue;
            WeatherName = App.GetService<IWeatherService>().GetWeatherTextByCode(c);
            WeatherColor = "";
            if (AllowColor)
            {
                if (WeatherName.Contains('雨') || WeatherName is "飑")
                    WeatherColor = "Rainy";
            }
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