using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.ViewModels;

namespace ClassIsland.Controls.NotificationProviders;

public partial class ClassNotificationProviderControl : UserControl, INotifyPropertyChanged
{
    private object? _element;
    private string _message = "";
    private int _slideIndex = 0;
    private bool _showTeacherName = false;
    private string _maskMessage = "";

    public object? Element
    {
        get => _element;
        set
        {
            if (Equals(value, _element)) return;
            _element = value;
            OnPropertyChanged();
        }
    }

    public string Message
    {
        get => _message;
        set
        {
            if (value == _message) return;
            _message = value;
            OnPropertyChanged();
        }
    }

    public int SlideIndex
    {
        get => _slideIndex;
        set
        {
            if (value == _slideIndex) return;
            _slideIndex = value;
            OnPropertyChanged();
        }
    }

    public bool ShowTeacherName
    {
        get => _showTeacherName;
        set
        {
            if (value == _showTeacherName) return;
            _showTeacherName = value;
            OnPropertyChanged();
        }
    }

    public string MaskMessage
    {
        get => _maskMessage;
        set
        {
            if (value == _maskMessage) return;
            _maskMessage = value;
            OnPropertyChanged();
        }
    }

    public ILessonsService LessonsService { get; } = App.GetService<ILessonsService>();

    private DispatcherTimer Timer { get; } = new()
    {
        Interval = TimeSpan.FromSeconds(10)
    };

    public ClassNotificationProviderControl(string key)
    {
        InitializeComponent();
        var visual = FindResource(key) as FrameworkElement;
        Element = visual;
        Timer.Tick += TimerOnTick;
        if (key is "ClassPrepareNotifyOverlay" or "ClassOffOverlay")
        {
            Timer.Start();
        }
        
        Unloaded += (_, _) => {
            Timer.Stop();
            Timer.Tick -= TimerOnTick;
        };
    }

    private void TimerOnTick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Message))
            return;
        SlideIndex = SlideIndex == 1 ? 0 : 1;
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

    public string NextTimeLayoutDurationHumanized =>
        FormatTimeSpan(LessonsService.CurrentTimeLayoutItem.Last);

    public static string FormatTimeSpan(TimeSpan span)
    {
        if (span.TotalSeconds <= 0) return "0 分钟";

        var parts = new List<string>(3);
        
        if (span.Hours > 0) parts.Add($"{span.Hours} 小时");
        if (span.Minutes > 0) parts.Add($"{span.Minutes} 分钟");
        if (span.Seconds > 0) parts.Add($"{span.Seconds} 秒");
    
        return string.Join(" ", parts);
    }
}
