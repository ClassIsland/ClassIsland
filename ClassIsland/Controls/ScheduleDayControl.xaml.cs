using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls;

/// <summary>
/// ScheduleDayControl.xaml 的交互逻辑
/// </summary>
public partial class ScheduleDayControl : UserControl
{
    public static readonly DependencyProperty DateProperty = DependencyProperty.Register(
        nameof(Date), typeof(DateTime), typeof(ScheduleDayControl), new PropertyMetadata(default(DateTime), (o, args) =>
        {
            if (o is ScheduleDayControl control)
            {
                control.UpdateData();
            }
        }));

    public DateTime Date
    {
        get { return (DateTime)GetValue(DateProperty); }
        set { SetValue(DateProperty, value); }
    }

    public static readonly DependencyProperty CurrentClassPlanProperty = DependencyProperty.Register(
        nameof(CurrentClassPlan), typeof(ClassPlan), typeof(ScheduleDayControl), new PropertyMetadata(default(ClassPlan)));

    public ClassPlan? CurrentClassPlan
    {
        get { return (ClassPlan)GetValue(CurrentClassPlanProperty); }
        set { SetValue(CurrentClassPlanProperty, value); }
    }

    public static readonly DependencyProperty IsClassPlanSelectionPopupOpenProperty = DependencyProperty.Register(
        nameof(IsClassPlanSelectionPopupOpen), typeof(bool), typeof(ScheduleDayControl), new PropertyMetadata(default(bool)));

    public bool IsClassPlanSelectionPopupOpen
    {
        get { return (bool)GetValue(IsClassPlanSelectionPopupOpenProperty); }
        set { SetValue(IsClassPlanSelectionPopupOpenProperty, value); }
    }

    public static readonly DependencyProperty SelectedClassPlanIdProperty = DependencyProperty.Register(
        nameof(SelectedClassPlanId), typeof(string), typeof(ScheduleDayControl), new PropertyMetadata(""));

    public string SelectedClassPlanId
    {
        get { return (string)GetValue(SelectedClassPlanIdProperty); }
        set { SetValue(SelectedClassPlanIdProperty, value); }
    }

    public static readonly DependencyProperty HasOrderedScheduleProperty = DependencyProperty.Register(
        nameof(HasOrderedSchedule), typeof(bool), typeof(ScheduleDayControl), new PropertyMetadata(default(bool)));

    public bool HasOrderedSchedule
    {
        get { return (bool)GetValue(HasOrderedScheduleProperty); }
        set { SetValue(HasOrderedScheduleProperty, value); }
    }

    public ILessonsService LessonsService { get; } = IAppHost.GetService<ILessonsService>();
    public IProfileService ProfileService { get; } = IAppHost.GetService<IProfileService>();

    private ScheduleCalendarControl? _parentScheduleCalendarControl;

    public ScheduleDayControl()
    {
        InitializeComponent();
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


    private void UIElement_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
    }

    private void ClassPlanSource_OnFilter(object sender, FilterEventArgs e)
    {
    }

    private void ListBoxTempClassPlanSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void ButtonConfirmTempClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        var date = Date;
        if (string.IsNullOrWhiteSpace(SelectedClassPlanId))
        {
            IsClassPlanSelectionPopupOpen = false;
            return;
        }
        ProfileService.Profile.OrderedSchedules[date] = new OrderedSchedule()
        {
            ClassPlanId = SelectedClassPlanId
        };
        IsClassPlanSelectionPopupOpen = false;
        UpdateData();
        e.Handled = true;
    }

    private void ButtonClearTempClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        var date = Date;
        ProfileService.Profile.OrderedSchedules.Remove(date);
        SelectedClassPlanId = "";
        IsClassPlanSelectionPopupOpen = false;
        UpdateData();
        e.Handled = true;
    }

    private void PopupCardRoot_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void UIElement_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void ButtonCloseSchedulePopup_OnClick(object sender, RoutedEventArgs e)
    {
        IsClassPlanSelectionPopupOpen = false;
        e.Handled = true;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void UIElement_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = false;
    }

    private void UIElement_OnPreviewMouseUp2(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void ButtonOrderSchedule_OnClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        IsClassPlanSelectionPopupOpen = true;
        UpdateData();
    }

    private void ScheduleDayControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        _parentScheduleCalendarControl = VisualTreeUtils.FindParentVisuals<ScheduleCalendarControl>(this).FirstOrDefault();
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