using ClassIsland.Core.Abstractions.Controls;
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
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Models;
using ClassIsland.Services;
using MaterialDesignThemes.Wpf;
using ClassIsland.Core.Models.AttachedSettings;
using ClassIsland.Shared.Abstraction.Models;

namespace ClassIsland.Controls.Components;

/// <summary>
/// ScheduleComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("1DB2017D-E374-4BC6-9D57-0B4ADF03A6B8", "课程表", PackIconKind.Schedule, "显示当前的课程表信息。")]
public partial class ScheduleComponent : INotifyPropertyChanged
{
    private bool _showCurrentLessonOnlyOnClass = false;
    public ILessonsService LessonsService { get; }

    public SettingsService SettingsService { get; }

    public IProfileService ProfileService { get; }

    public bool ShowCurrentLessonOnlyOnClass
    {
        get => _showCurrentLessonOnlyOnClass;
        set
        {
            if (value == _showCurrentLessonOnlyOnClass) return;
            _showCurrentLessonOnlyOnClass = value;
            OnPropertyChanged();
        }
    }


    public ScheduleComponent(ILessonsService lessonsService, SettingsService settingsService, IProfileService profileService)
    {
        LessonsService = lessonsService;
        SettingsService = settingsService;
        ProfileService = profileService;
        LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        InitializeComponent();
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        var settingsSource =
            (ILessonControlSettings?)IAttachedSettingsHostService.GetAttachedSettingsByPriority<LessonControlAttachedSettings>(
                new Guid("58e5b69a-764a-472b-bcf7-003b6a8c7fdf"),
                LessonsService.CurrentSubject,
                LessonsService.CurrentTimeLayoutItem,
                LessonsService.CurrentClassPlan,
                LessonsService.CurrentClassPlan?.TimeLayout) ??
            Settings;
        ShowCurrentLessonOnlyOnClass = settingsSource.ShowCurrentLessonOnlyOnClass;
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