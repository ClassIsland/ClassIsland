using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Controls;
using ClassIsland.Converters;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.ViewModels;
using MaterialDesignThemes.Wpf;
using Microsoft.AppCenter.Analytics;
using Application = System.Windows.Application;
using Path = System.IO.Path;

namespace ClassIsland.Views;
/// <summary>
/// ProfileSettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class ProfileSettingsWindow : MyWindow
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

    public bool IsOpened
    {
        get;
        set;
    } = false;

    public AttachedSettingsHostService AttachedSettingsHostService { get; } =
        App.GetService<AttachedSettingsHostService>();

    public void OpenDrawer(string key)
    {
        ViewModel.DrawerContent = FindResource(key);
        var r = key switch
        {
            "TemporaryClassPlan" => "档案设置 · 打开临时课表设置",
            _ => null
        };
        if (r != null)
        {
            Analytics.TrackEvent(r);
        }
        DrawerHost.OpenDrawerCommand.Execute(null, MyDrawerHost);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        var d = (DictionaryValueAccessConverter)FindResource("DictionaryValueAccessConverter");
        d.SourceDictionary = MainViewModel.Profile.Subjects;
        var d2 = (ClassPlanDictionaryValueAccessConverter)FindResource("ClassPlanDictionaryValueAccessConverter");
        d2.SourceDictionary = MainViewModel.Profile.TimeLayouts;

        MainViewModel.Settings.PropertyChanged += SettingsOnPropertyChanged;
        RefreshProfiles();

        base.OnInitialized(e);
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.Settings.SelectedProfile):
                ViewModel.IsRestartSnackbarActive = true;
                break;
        }
    }

    private void RefreshProfiles()
    {
        ViewModel.Profiles = new ObservableCollection<string>
            (from i in Directory.GetFiles("./Profiles")
            where i.EndsWith(".json")
            select Path.GetFileName(i));
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
        Analytics.TrackEvent("档案设置 · 创建新时间表");
    }

    private void ButtonAddClassTime_OnClick(object sender, RoutedEventArgs e)
    {
        AddTimeLayoutItem(0);
    }

    private void AddTimeLayoutItem(int timeType)
    {
        var timeLayout = ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Value;
        var selected = (TimeLayoutItem?)ListViewTimePoints.SelectedValue;
        var newItem = new TimeLayoutItem()
        {
            TimeType = timeType,
            StartSecond = selected?.EndSecond ?? DateTime.Now,
            EndSecond = selected?.EndSecond ?? DateTime.Now
        };
        timeLayout.Layouts.Add(newItem);
        ListViewTimePoints.SelectedValue = newItem;
        UpdateTimeLayout();
        OpenDrawer("TimePointEditor");
        Analytics.TrackEvent("档案设置 · 创建时间点", new Dictionary<string, string>
        {
            {"Type", timeType.ToString()}
        });
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
        Analytics.TrackEvent("档案设置 · 编辑时间点");
    }

    private void ButtonEditTimeLayoutInfo_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DrawerContent = FindResource("TimeLayoutInfoEditor");
        Analytics.TrackEvent("档案设置 · 编辑时间表信息");
    }

    private void ButtonRemoveTimePoint_OnClick(object sender, RoutedEventArgs e)
    {
        if (ListViewTimePoints.SelectedValue != null)
        {
            ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedValue).Value.Layouts.Remove((TimeLayoutItem)ListViewTimePoints.SelectedValue);
        }

        UpdateTimeLayout();
        Analytics.TrackEvent("档案设置 · 删除时间点");
    }

    private async void ButtonDeleteTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        var c = (from i in MainViewModel.Profile.ClassPlans
            where i.Value.TimeLayoutId == ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Key
            select i.Value).Count();
        var eventName = "档案设置 · 删除时间表";
        if (c > 0)
        {
            ViewModel.MessageQueue.Enqueue("仍有课表在使用该时间表。删除时间表前需要删除所有使用该时间表的课表。");
            Analytics.TrackEvent(eventName, new Dictionary<string, string>
            {
                {"IsSuccess", "false"},
                {"Reason", "仍有课表在使用该时间表。"}
            });
            return;
        }

        var r = (bool?)await DialogHost.Show(FindResource("DeleteTimeLayoutConfirm"), dialogIdentifier: ViewModel.DialogHostId);
        if (r == true)
        {
            Analytics.TrackEvent(eventName, new Dictionary<string, string>
            {
                {"IsSuccess", "true"}
            });
            MainViewModel.Profile.TimeLayouts.Remove(((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Key);
        }
        else
        {
            Analytics.TrackEvent(eventName, new Dictionary<string, string>
            {
                {"IsSuccess", "false"},
                {"Reason", "用户取消操作。"}
            });
        }
    }

    private void ButtonAddSubject_OnClick(object sender, RoutedEventArgs e)
    {
        MainViewModel.Profile.Subjects.Add(Guid.NewGuid().ToString(), new Subject());
        ListViewSubjects.SelectedIndex = MainViewModel.Profile.Subjects.Count - 1;
        TextBoxSubjectName.Focus();
        Analytics.TrackEvent("档案设置 · 添加科目");
    }

    private async void ButtonSubject_OnClick(object sender, RoutedEventArgs e)
    {
        var r = (bool?)await DialogHost.Show(FindResource("DeleteSubjectConfirm"),dialogIdentifier: ViewModel.DialogHostId);
        if (r == true)
        {
            Analytics.TrackEvent("档案设置 · 删除科目", new Dictionary<string, string>
            {
                {"IsSuccess", "true"},
            });
            MainViewModel.Profile.Subjects.Remove(((KeyValuePair<string, Subject>)ListViewSubjects.SelectedItem).Key);
        }
        else
        {
            Analytics.TrackEvent("档案设置 · 删除科目", new Dictionary<string, string>
            {
                {"IsSuccess", "false"},
                {"Reason", "用户取消操作。"}
            });
        }
    }

    private void ButtonAddClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        Analytics.TrackEvent("档案设置 · 添加课表");
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
        Analytics.TrackEvent("档案设置 · 编辑课表信息");
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
        var r = (bool?)await DialogHost.Show(FindResource("DeleteClassPlanConfirm"), dialogIdentifier: ViewModel.DialogHostId);
        if (r == true)
        {
            Analytics.TrackEvent("档案设置 · 删除课表", new Dictionary<string, string>
            {
                {"IsSuccess", "true"}
            });
            MainViewModel.Profile.ClassPlans.Remove(((KeyValuePair<string, ClassPlan>)ListViewClassPlans.SelectedItem).Key);
        }
        else
        {
            Analytics.TrackEvent("档案设置 · 删除课表", new Dictionary<string, string>
            {
                {"IsSuccess", "false"},
                {"Reason", "用户取消操作"}
            });
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
        Analytics.TrackEvent("档案设置 · 复制课表");
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
        Analytics.TrackEvent("档案设置 · 复制时间表");
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
        Analytics.TrackEvent("档案设置 · 复制科目");
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
        e.Cancel = true;
        if (!ViewModel.IsClassPlansEditing)
        {
            Hide();
            IsOpened = false;
        }
    }

    private void ButtonTemporaryClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DrawerContent = FindResource("TemporaryClassPlan");
        Analytics.TrackEvent("档案设置 · 打开临时课表设置");
    }

    private void ButtonClearTemporaryClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        MainViewModel.TemporaryClassPlan = null;
    }

    private void ListBoxTempClassPlanSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MainViewModel.TemporaryClassPlanSetupTime = DateTime.Now;
    }

    private void TabControlSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var c = (KeyValuePair<string, ClassPlan>?)ListViewClassPlans.SelectedValue;
        c?.Value.RefreshClassesList();
    }

    private void ButtonProfileManage_OnClick(object sender, RoutedEventArgs e)
    {
        Analytics.TrackEvent("档案设置 · 打开档案管理");
        OpenDrawer("ProfileManager");
    }

    private void SnackbarRestartMessage_OnActionClick(object sender, RoutedEventArgs e)
    {
        Analytics.TrackEvent("重启应用", new Dictionary<string, string>()
        {
            {"Source", "档案管理重启"}
        });
        var mw = (MainWindow)Application.Current.MainWindow!;
        mw.SaveProfile();
        mw.SaveSettings();
        App.ReleaseLock();
        Application.Current.Shutdown();
        System.Windows.Forms.Application.Restart();
    }

    private async void ButtonCreateProfile_OnClick(object sender, RoutedEventArgs e)
    {
        Analytics.TrackEvent("档案管理 · 创建档案");
        ViewModel.CreateProfileName = "";
        var r = await DialogHost.Show(FindResource("CreateProfileDialog"), ViewModel.DialogHostId);
        Debug.WriteLine(r);

        var path = $"./Profiles/{r}.json";
        if (r == null || File.Exists(path))
        {
            return;
        }

        var json = JsonSerializer.Serialize(new Profile());
        await File.WriteAllTextAsync(path, json);
        RefreshProfiles();
    }

    private void ButtonOpenProfileFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Analytics.TrackEvent("档案管理 · 打开档案文件夹");
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath("./Profiles/"),
            UseShellExecute = true
        });
    }

    private void ButtonRefreshProfiles_OnClick(object sender, RoutedEventArgs e)
    {
        Analytics.TrackEvent("档案管理 · 刷新档案");
        RefreshProfiles();
    }

    private async void MenuItemRenameProfile_OnClick(object sender, RoutedEventArgs e)
    {
        Analytics.TrackEvent("档案管理 · 重命名档案");
        ViewModel.RenameProfileName = Path.GetFileNameWithoutExtension(ViewModel.SelectedProfile);
        var r = await DialogHost.Show(FindResource("RenameProfileDialog"), ViewModel.DialogHostId);
        Debug.WriteLine(r);

        var raw = $"./Profiles/{ViewModel.SelectedProfile}";
        var path = $"./Profiles/{r}.json";
        if (r == null || !File.Exists(raw) || File.Exists(path))
        {
            return;
        }

        File.Move(raw, path);
        if (MainViewModel.CurrentProfilePath == Path.GetFileName(raw))
        {
            MainViewModel.CurrentProfilePath = Path.GetFileName(path);
            MainViewModel.Settings.SelectedProfile = Path.GetFileName(path);
        }
        RefreshProfiles();
    }

    private async void MenuItemDeleteProfile_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteConfirmField = "";
        var path = $"./Profiles/{ViewModel.SelectedProfile}";
        if (ViewModel.SelectedProfile == MainViewModel.CurrentProfilePath ||
            ViewModel.SelectedProfile == MainViewModel.Settings.SelectedProfile)
        {
            Analytics.TrackEvent("档案管理 · 删除档案", new Dictionary<string, string>
            {
                {"Reason", "正在删除已加载或将要加载的档案。"},
                {"IsSuccess", "false"}
            });
            ViewModel.MessageQueue.Enqueue("无法删除已加载或将要加载的档案。");
            return;
        }
        var r = await DialogHost.Show(FindResource("DeleteProfileDialog"), ViewModel.DialogHostId);
        Debug.WriteLine(r);

        if ((bool?)r == true)
        {
            Analytics.TrackEvent("档案管理 · 删除档案", new Dictionary<string, string>
            {
                {"IsSuccess", "true"}
            });
            File.Delete(path);
        }
        else
        {
            Analytics.TrackEvent("档案管理 · 删除档案", new Dictionary<string, string>
            {
                {"Reason", "用户取消操作。"},
                {"IsSuccess", "false"}
            });
        }
        RefreshProfiles();
    }

    private void MenuItemProfileDuplicate_OnClick(object sender, RoutedEventArgs e)
    {
        Analytics.TrackEvent("档案管理 · 复制档案");
        var raw = $"./Profiles/{ViewModel.SelectedProfile}";
        var d = Path.GetFileNameWithoutExtension(ViewModel.SelectedProfile) + " - 副本.json";
        var d1 = $"./Profiles/{d}";
        File.Copy(raw, d1);
        RefreshProfiles();
    }

    public static async void OpenFromFile(string path)
    {
        Analytics.TrackEvent("档案设置 · 从离线文件读取档案");
        var o = JsonSerializer.Deserialize<Profile>(await File.ReadAllTextAsync(path));
        if (o == null)
        {
            return;
        }
        var pw = new ProfileSettingsWindow()
        {
            MainViewModel =
            {
                Profile = o,
                CurrentProfilePath = Path.GetFileName(path)
            },
            ViewModel =
            {
                IsOfflineEditor = true
            }
        };
        pw.ShowDialog();
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(o));
        GC.Collect();
    }

    private void MenuItemProfileEdit_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProfile == MainViewModel.CurrentProfilePath)
        {
            Analytics.TrackEvent("档案管理 · 编辑档案", new Dictionary<string, string>
            {
                {"Reason", "无法编辑已加载的档案。"},
                {"IsSuccess", "false"}
            });
            ViewModel.MessageQueue.Enqueue("无法编辑已加载的档案。");
            return;
        }
        Analytics.TrackEvent("档案管理 · 编辑档案", new Dictionary<string, string>
        {
            {"IsSuccess", "true"}
        });
        OpenFromFile($"./Profiles/{ViewModel.SelectedProfile}");
    }

    private void TimePointDoubleClick_OnHandler(object sender, MouseButtonEventArgs e)
    {
        OpenDrawer("TimePointEditor");
        Analytics.TrackEvent("档案设置 · 编辑时间点");
    }
}
