using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
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
        ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Value.Layouts.Add(new TimeLayoutItem()
        {
            TimeType = 0
        });
    }

    private void ButtonAddBreakTime_OnClick(object sender, RoutedEventArgs e)
    {
        ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Value.Layouts.Add(new TimeLayoutItem()
        {
            TimeType = 1
        });
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
}
