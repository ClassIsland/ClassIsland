using System;
using System.Collections.Generic;
using System.Linq;
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
        e.Handled = false;
        IsClassPlanSelectionPopupOpen = true;
        UpdateData();
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
}