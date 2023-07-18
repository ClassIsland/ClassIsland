using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Converters;
using ClassIsland.Models;
using ClassIsland.ViewModels;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Views;
/// <summary>
/// ProfileSettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class ProfileSettingsWindow : Window
{
    public MainViewModel MainViewModel
    {
        get;
        set;
    } = new();

    public ProfileSettingsViewModel ViewModel
    {
        get;
        set;
    } = new();

    public ProfileSettingsWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        var d = (DictionaryValueAccessConverter)FindResource("DictionaryValueAccessConverter");
        d.SourceDictionary = MainViewModel.Profile.Subjects;
        var d2 = (ClassPlanDictionaryValueAccessConverter)FindResource("ClassPlanDictionaryValueAccessConverter");
        d2.SourceDictionary = MainViewModel.Profile.TimeLayouts;

        base.OnInitialized(e);
    }

    private void ButtonAddTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        MainViewModel.Profile.TimeLayouts.Add(Guid.NewGuid().ToString(), new TimeLayout()
        {
            Name = "新时间表"
        });
        //MainViewModel.Profile.NotifyPropertyChanged(nameof(MainViewModel.Profile.TimeLayouts));
        ViewModel.DrawerContent = FindResource("TimeLayoutInfoEditor");
        ListViewTimeLayouts.SelectedIndex = MainViewModel.Profile.TimeLayouts.Count - 1;
    }

    private void ButtonAddClassTime_OnClick(object sender, RoutedEventArgs e)
    {
        AddTimeLayoutItem(0);
    }

    private void AddTimeLayoutItem(int timeType)
    {
        var timeLayout = ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Value;
        timeLayout.Layouts.Add(new TimeLayoutItem()
        {
            TimeType = timeType
        });
        UpdateTimeLayout();
    }

    private void UpdateTimeLayout()
    {
        var timeLayout = ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Value;
        var l = timeLayout.Layouts.ToList();
        l.Sort();
        l.Reverse();
        timeLayout.Layouts = new ObservableCollection<TimeLayoutItem>(l);
    }

    private void ButtonAddBreakTime_OnClick(object sender, RoutedEventArgs e)
    {
        AddTimeLayoutItem(1);
    }

    private void ButtonEditTimePoint_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DrawerContent = FindResource("TimePointEditor");
    }

    private void ButtonEditTimeLayoutInfo_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DrawerContent = FindResource("TimeLayoutInfoEditor");
    }

    private void ButtonRemoveTimePoint_OnClick(object sender, RoutedEventArgs e)
    {
        if (ListViewTimePoints.SelectedItem != null)
        {
            ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Value.Layouts.Remove((TimeLayoutItem)ListViewTimePoints.SelectedItem);
        }

        UpdateTimeLayout();
    }

    private async void ButtonDeleteTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        var r = (bool?)await DialogHost.Show(FindResource("DeleteTimeLayoutConfirm"), dialogIdentifier: "ProfileWindow");
        if (r == true)
        {
            MainViewModel.Profile.TimeLayouts.Remove(((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Key);
        }
    }

    private void ButtonAddSubject_OnClick(object sender, RoutedEventArgs e)
    {
        MainViewModel.Profile.Subjects.Add(Guid.NewGuid().ToString(), new Subject());
        ListViewSubjects.SelectedIndex = MainViewModel.Profile.Subjects.Count - 1;
    }

    private async void ButtonSubject_OnClick(object sender, RoutedEventArgs e)
    {
        var r = (bool?)await DialogHost.Show(FindResource("DeleteSubjectConfirm"),dialogIdentifier: "ProfileWindow");
        if (r == true)
        {
            MainViewModel.Profile.Subjects.Remove(((KeyValuePair<string, Subject>)ListViewSubjects.SelectedItem).Key);
        }
    }

    private void ButtonAddClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        MainViewModel.Profile.ClassPlans.Add(Guid.NewGuid().ToString(), new ClassPlan());
        ListViewClassPlans.SelectedIndex = MainViewModel.Profile.ClassPlans.Count - 1;
        ViewModel.DrawerContent = FindResource("ClassPlansInfoEditor");
    }

    private void ButtonDebugAddNewClass_OnClick(object sender, RoutedEventArgs e)
    {
        var s = (KeyValuePair<string, ClassPlan>?)ListViewClassPlans.SelectedItem;
        s?.Value.Classes.Add(new ClassInfo());
    }

    private void ButtonClassPlanInfoEdit_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DrawerContent = FindResource("ClassPlansInfoEditor");
    }

    private void ListViewClassPlans_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (KeyValuePair<string, ClassPlan> i in e.AddedItems)
        {
            i.Value.RefreshClassesList();
        }
    }

    private async void ButtonDeleteClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        var r = (bool?)await DialogHost.Show(FindResource("DeleteClassPlanConfirm"), dialogIdentifier: "ProfileWindow");
        if (r == true)
        {
            MainViewModel.Profile.ClassPlans.Remove(((KeyValuePair<string, ClassPlan>)ListViewClassPlans.SelectedItem).Key);
        }
    }

    private void ButtonRulesEdit_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DrawerContent = FindResource("ClassPlanRulesEditor");
    }

    private void DrawerHost_OnDrawerClosing(object? sender, DrawerClosingEventArgs e)
    {
        var timeLayoutItemEdit = FindResource("TimePointEditor");
        if (ViewModel.DrawerContent == timeLayoutItemEdit)
        {
            UpdateTimeLayout();
        }
    }

    private void DataGridClassPlans_OnUnloadingRow(object? sender, DataGridRowEventArgs e)
    {
        DataGridClassPlans.CommitEdit(DataGridEditingUnit.Cell, true);
    }

    private void ButtonAddSeparator_OnClick(object sender, RoutedEventArgs e)
    {
        AddTimeLayoutItem(2);
    }

    private void ButtonDuplicateClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        var s = CopyObject(((KeyValuePair<string, ClassPlan>)ListViewClassPlans.SelectedItem).Value);
        if (s == null)
        {
            return;
        }

        ViewModel.DrawerContent = FindResource("ClassPlansInfoEditor");
        MainViewModel.Profile.ClassPlans.Add(Guid.NewGuid().ToString(), s);
        ListViewClassPlans.SelectedItem = MainViewModel.Profile.ClassPlans.Last();
    }

    private T? CopyObject<T>(T o) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize<T>(o));

    private void ButtonDuplicateTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        var s = CopyObject(((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Value);
        if (s == null)
        {
            return;
        }

        ViewModel.DrawerContent = FindResource("TimeLayoutInfoEditor");
        MainViewModel.Profile.TimeLayouts.Add(Guid.NewGuid().ToString(), s);
        ListViewTimeLayouts.SelectedItem = MainViewModel.Profile.TimeLayouts.Last();
    }

    private void ButtonDuplicateSubject_OnClick(object sender, RoutedEventArgs e)
    {
        var s = CopyObject(((KeyValuePair<string, Subject>)ListViewSubjects.SelectedItem).Value);
        if (s == null)
        {
            return;
        }
        
        MainViewModel.Profile.Subjects.Add(Guid.NewGuid().ToString(), s);
        ListViewTimeLayouts.SelectedItem = MainViewModel.Profile.Subjects.Last();
    }

    private void DataGridClassPlans_OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        ViewModel.IsClassPlansEditing = true;
        var hWnd = new WindowInteropHelper(this).Handle;
        NativeWindowHelper.SetWindowLong(hWnd, NativeWindowHelper.GWL_STYLE, 
            NativeWindowHelper.GetWindowLong(hWnd, NativeWindowHelper.GWL_STYLE) & ~NativeWindowHelper.WS_SYSMENU);
    }

    private void DataGridClassPlans_OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        ViewModel.IsClassPlansEditing = false;
        var hWnd = new WindowInteropHelper(this).Handle;
        NativeWindowHelper.SetWindowLong(hWnd, NativeWindowHelper.GWL_STYLE,
            NativeWindowHelper.GetWindowLong(hWnd, NativeWindowHelper.GWL_STYLE) | NativeWindowHelper.WS_SYSMENU);
    }

    private void ProfileSettingsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (ViewModel.IsClassPlansEditing)
        {
            e.Cancel = true;
        }
    }
}
