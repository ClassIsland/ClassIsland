using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls;

/// <summary>
/// ScheduleDayControl.xaml 的交互逻辑
/// </summary>
public partial class ScheduleDayControl : UserControl
{
    public static readonly StyledProperty<DateTime> DateProperty = AvaloniaProperty.Register<ScheduleDayControl, DateTime>(
        nameof(Date));

    public DateTime Date
    {
        get => GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public static readonly StyledProperty<ClassPlan?> CurrentClassPlanProperty = AvaloniaProperty.Register<ScheduleDayControl, ClassPlan?>(
        nameof(CurrentClassPlan), new ClassPlan()
        {
            Name = ""
        });

    public ClassPlan? CurrentClassPlan
    {
        get => GetValue(CurrentClassPlanProperty);
        set => SetValue(CurrentClassPlanProperty, value);
    }

    public static readonly StyledProperty<Guid> SelectedClassPlanIdProperty = AvaloniaProperty.Register<ScheduleDayControl, Guid>(
        nameof(SelectedClassPlanId));

    public Guid SelectedClassPlanId
    {
        get => GetValue(SelectedClassPlanIdProperty);
        set => SetValue(SelectedClassPlanIdProperty, value);
    }

    public static readonly StyledProperty<bool> HasOrderedScheduleProperty = AvaloniaProperty.Register<ScheduleDayControl, bool>(
        nameof(HasOrderedSchedule));

    public bool HasOrderedSchedule
    {
        get => GetValue(HasOrderedScheduleProperty);
        set => SetValue(HasOrderedScheduleProperty, value);
    }

    public static readonly StyledProperty<SyncDictionaryList<Guid, ClassPlan>> ClassPlanListProperty = AvaloniaProperty.Register<ScheduleDayControl, SyncDictionaryList<Guid, ClassPlan>>(
        nameof(ClassPlanList));

    public SyncDictionaryList<Guid, ClassPlan> ClassPlanList
    {
        get => GetValue(ClassPlanListProperty);
        set => SetValue(ClassPlanListProperty, value);
    }

    public ILessonsService LessonsService { get; } = IAppHost.GetService<ILessonsService>();
    public IProfileService ProfileService { get; } = IAppHost.GetService<IProfileService>();

    private ScheduleCalendarControl? _parentScheduleCalendarControl;

    public ScheduleDayControl()
    {
        InitializeComponent();

        this.GetObservable(DateProperty).Subscribe(_ => UpdateData());
    }

    private void UpdateData()
    {
        CurrentClassPlan = LessonsService.GetClassPlanByDate(Date);
        if (ProfileService.Profile.OrderedSchedules.TryGetValue(Date.Date, out var orderedSchedule))
        {
            HasOrderedSchedule = true;
            SelectedClassPlanId = orderedSchedule.ClassPlanId;
        }
        else
        {
            HasOrderedSchedule = false;
        }
    }

    private void ButtonConfirmTempClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        var date = Date;
        if (SelectedClassPlanId == Guid.Empty)
        {
            FlyoutHelper.CloseAncestorFlyout(sender);
            return;
        }
        ProfileService.Profile.OrderedSchedules[date] = new OrderedSchedule()
        {
            ClassPlanId = SelectedClassPlanId
        };
        FlyoutHelper.CloseAncestorFlyout(sender);
        UpdateData();
        e.Handled = true;
    }

    private void ButtonClearTempClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        var date = Date;
        ProfileService.Profile.OrderedSchedules.Remove(date);
        SelectedClassPlanId = Guid.Empty;
        FlyoutHelper.CloseAncestorFlyout(sender);
        UpdateData();
        e.Handled = true;
    }
    
    private void ButtonCloseSchedulePopup_OnClick(object sender, RoutedEventArgs e)
    {
        FlyoutHelper.CloseAncestorFlyout(sender);
        e.Handled = true;
    }
    

    private void ButtonOrderSchedule_OnClick(object sender, RoutedEventArgs e)
    {
        ClassPlanList = ScheduleCalendarControl.GetClassPlanList(this);
        UpdateData();
    }

    private void ScheduleDayControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        _parentScheduleCalendarControl = this.FindAncestorOfType<ScheduleCalendarControl>();
        if (_parentScheduleCalendarControl != null)
        {
            _parentScheduleCalendarControl.ScheduleUpdated += ParentScheduleCalendarControlOnScheduleUpdated;
        }
    }

    private void ParentScheduleCalendarControlOnScheduleUpdated(object? sender, EventArgs e)
    {
        UpdateData();
    }

    private void ScheduleDayControl_OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_parentScheduleCalendarControl != null)
        {
            _parentScheduleCalendarControl.ScheduleUpdated -= ParentScheduleCalendarControlOnScheduleUpdated;
        }

        _parentScheduleCalendarControl = null;
    }
}