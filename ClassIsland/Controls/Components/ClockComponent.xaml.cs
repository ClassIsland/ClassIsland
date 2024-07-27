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
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.ComponentSettings;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.Components;

/// <summary>
/// ClockComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("9E1AF71D-8F77-4B21-A342-448787104DD9", "时钟", PackIconKind.ClockDigital, "显示现在的时间，支持精确到秒。")]
public partial class ClockComponent : ComponentBase<ClockComponentSettings>, INotifyPropertyChanged
{
    private string _currentTime = "";
    public ILessonsService LessonsService { get; }

    public IExactTimeService ExactTimeService { get; }

    public string CurrentTime
    {
        get => _currentTime;
        set
        {
            if (value == _currentTime) return;
            _currentTime = value;
            OnPropertyChanged();
        }
    }

    public ClockComponent(ILessonsService lessonsService, IExactTimeService exactTimeService)
    {
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;
        InitializeComponent();

        LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        var time = ExactTimeService.GetCurrentLocalDateTime();
        CurrentTime = Settings.ShowSeconds ? time.ToLongTimeString() :
                      time.Second % 2 == 1 ? time.ToShortTimeString() : time.ToShortTimeString().Replace(":", " ");
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