using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using ClassIsland.Core.Models.Weather;

namespace ClassIsland.Controls.NotificationProviders;

/// <summary>
/// WeatherNotificationProviderControl.xaml 的交互逻辑
/// </summary>
public partial class WeatherNotificationProviderControl : UserControl, INotifyPropertyChanged
{
    private WeatherAlert _alert = new WeatherAlert();
    private bool _isOverlay;

    public bool IsOverlay
    {
        get => _isOverlay;
        set
        {
            if (value == _isOverlay) return;
            _isOverlay = value;
            OnPropertyChanged();
        }
    }

    public WeatherAlert Alert
    {
        get => _alert;
        set
        {
            if (Equals(value, _alert)) return;
            _alert = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan Duration { get; }

    public WeatherNotificationProviderControl(bool isOverlay, WeatherAlert alert, TimeSpan duration)
    {
        InitializeComponent();
        IsOverlay = isOverlay;
        Alert = alert;
        Duration = duration;
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

    private void WeatherNotificationProviderControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        var visual = ElementComposition.GetElementVisual(Description);
        if (visual == null)
        {
            return;
        }

        var compositor = visual.Compositor;
        var anim = compositor.CreateVector3DKeyFrameAnimation();
        anim.Target = nameof(visual.Offset);
        anim.Duration = Duration;
        anim.IterationBehavior = AnimationIterationBehavior.Count;
        anim.IterationCount = 2;
        anim.InsertKeyFrame(0f, visual.Offset with { X = RootCanvas.Bounds.Width }, new LinearEasing());
        anim.InsertKeyFrame(1f, visual.Offset with { X = -Description.Bounds.Width }, new LinearEasing());
        visual.StartAnimation(nameof(visual.Offset), anim);
    }
}