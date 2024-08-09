using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;

using ClassIsland.Controls;
using ClassIsland.Converters;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Converters;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Services;
using ClassIsland.Services.Management;
using ClassIsland.ViewModels;

using MaterialDesignThemes.Wpf;

using Sentry;
using Application = System.Windows.Application;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using Path = System.IO.Path;
using TabControl = System.Windows.Controls.TabControl;

namespace ClassIsland.Views;
/// <summary>
/// ProfileSettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class ProfileSettingsWindow : MyWindow
{
    public static RoutedUICommand OpenTimeLayoutItemEditorCommand = new RoutedUICommand();

    public static RoutedUICommand RemoveSelectedTimeLayoutItemCommand = new RoutedUICommand();

    public IHangService HangService { get; } = App.GetService<IHangService>();

    public IManagementService ManagementService { get; } = App.GetService<IManagementService>();

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

    public IAttachedSettingsHostService AttachedSettingsHostService { get; } =
        App.GetService<IAttachedSettingsHostService>();

    public ILessonsService LessonsService { get; } = App.GetService<ILessonsService>();

    public IProfileService ProfileService { get; } = App.GetService<IProfileService>();

    public void OpenDrawer(string key)
    {
        ViewModel.DrawerContent = FindResource(key);
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.drawers.open", tags: new Dictionary<string, string>
        {
            {"key", key}
        });
        DrawerHost.OpenDrawerCommand.Execute(null, MyDrawerHost);
    }

    public void OpenTimeLayoutEdit(string? key="")
    {
        RootTabControl.SelectedIndex = 1;

        if (key != null)
        {
            ListViewTimeLayouts.SelectedItem =
                new KeyValuePair<string, TimeLayout>(key, ProfileService.Profile.TimeLayouts[key]);
        }
    }

    protected override void OnContentRendered(EventArgs e)
    {
        var d = (SubjectsDictionaryValueAccessConverter)FindResource("DictionaryValueAccessConverter");
        d.SourceDictionary = MainViewModel.Profile.Subjects;
        var d2 = (ClassPlanDictionaryValueAccessConverter)FindResource("ClassPlanDictionaryValueAccessConverter");
        d2.SourceDictionary = MainViewModel.Profile.TimeLayouts;
        TabTimeLayoutEditors.SelectedIndex = App.GetService<SettingsService>().Settings.TimeLayoutEditorIndex;

        MainViewModel.Settings.PropertyChanged += SettingsOnPropertyChanged;
        RefreshProfiles();

        base.OnContentRendered(e);
    }

    private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled)
        {
            // ListView拦截鼠标滚轮事件
            e.Handled = true;

            // 激发一个鼠标滚轮事件，冒泡给外层ListView接收到
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            var parent = ((System.Windows.Controls.Control)sender).Parent as UIElement;
            if (parent != null)
            {
                parent.RaiseEvent(eventArg);
            }
        }
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
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.timeLayout.create");
    }

    private void ButtonAddClassTime_OnClick(object sender, RoutedEventArgs e)
    {
        AddTimeLayoutItem(0);
    }

    private void AddTimeLayoutItem(int timeType)
    {
        var timeLayout = ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Value;
        var selected   = (TimeLayoutItem?)ListViewTimePoints.SelectedValue;
        var baseSec    = selected?.EndSecond ?? DateTime.Today + new TimeSpan(7, 30, 0);
        var settings   = App.GetService<SettingsService>().Settings;
        var lastTime   = TimeSpan.FromMinutes(timeType switch
        {
            0 => settings.DefaultOnClassTimePointMinutes,
            1 => settings.DefaultBreakingTimePointMinutes,
            2 => 0,
            _ => 0
        });
        if (selected != null)
        {
            var index = timeLayout.Layouts.IndexOf(selected);
            /*if (selected.TimeType == 2)
            {
                // 向前的非线时间段集合
                // var l = (from i in timeLayout.Layouts.Take(index + 1) where i.TimeType != 2 select i).ToList();
                selected = l.Count > 0 ? l.Last() : selected;
            }*/
            if (timeType != 2 && index < timeLayout.Layouts.Count - 1)
            {
                var nexts = (from i 
                            in timeLayout.Layouts.Skip(index + 1) 
                        where i.TimeType != 2 
                        select i)
                    .ToList();
                if (nexts.Count > 0)
                {
                    var next = nexts[0];
                    if (next.StartSecond.TimeOfDay <= baseSec.TimeOfDay)
                    {
                        if (index != 0)
                        {
                            ViewModel.MessageQueue.Enqueue("没有合适的位置来插入新的时间点。");
                            return;
                        }
                        baseSec = selected.StartSecond - lastTime; // 向前插入时间点的简易实现，未考虑分割线
                        ViewModel.MessageQueue.Enqueue("已向前插入了新的时间点。");
                    }
                    if (next.StartSecond.TimeOfDay < baseSec.TimeOfDay + lastTime)
                    {
                        ViewModel.MessageQueue.Enqueue("没有足够的空间完全插入该时间点，已缩短时间点长度。");
                        lastTime = next.StartSecond.TimeOfDay - baseSec.TimeOfDay;
                    }
                }
            }

            if (timeType == 2)
            {
                baseSec = selected.EndSecond;
                if ((from i in timeLayout.Layouts where i.TimeType == 2 select i.StartSecond).ToList().Contains(baseSec))
                {
                    ViewModel.MessageQueue.Enqueue("这里已经存在一条分割线。");
                    return;
                }
            }
        }
        var newItem = new TimeLayoutItem()
        {
            TimeType = timeType,
            StartSecond = baseSec,
            EndSecond = baseSec + lastTime,
        };
        AddTimePoint(newItem);
        // ReSortTimeLayout(newItem);
        ListViewTimePoints.SelectedValue = newItem;
        //OpenDrawer("TimePointEditor");
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.timePoint.create", tags: new Dictionary<string, string>()
        {
            {"Type", timeType.ToString()},
            {"Auto", "False"}
        });
    }

    public void AddTimeLayoutItem(int timeType, DateTime startTime, DateTime endTime)
    {
        var newItem = new TimeLayoutItem
        {
            TimeType    = timeType,
            StartSecond = startTime,
            EndSecond   = endTime,
        };
        AddTimePoint(newItem);
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.timePoint.create", tags: new Dictionary<string, string>()
        {
            {"Type", timeType.ToString()},
            {"Auto", "True"}
        });
    }

    public void UpdateTimeLayout()
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
        OpenDrawer("TimePointEditor");
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.timePoint.edit");
    }

    private void ButtonEditTimeLayoutInfo_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("TimeLayoutInfoEditor");
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.timeLayout.edit");
    }

    private void ButtonRemoveTimePoint_OnClick(object sender, RoutedEventArgs e)
    {
        if (ListViewTimePoints.SelectedValue is not TimeLayoutItem timePoint) 
            return;
        var timeLayout = ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedValue).Value;
        var i = timeLayout.Layouts.IndexOf(timePoint);
        timeLayout.RemoveTimePoint(timePoint);
        UpdateTimeLayout();
        if (i > 0)
            ViewModel.SelectedTimePoint = timeLayout.Layouts[i - 1];
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.timePoint.remove");

    }

    private async void ButtonDeleteTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        var c = (from i in MainViewModel.Profile.ClassPlans
            where i.Value.TimeLayoutId == ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Key
            select i.Value).Count();
        var eventName = "views.ProfileSettingsWindow.timeLayout.remove";
        if (c > 0)
        {
            ViewModel.MessageQueue.Enqueue("仍有课表在使用该时间表。删除时间表前需要删除所有使用该时间表的课表。");
            SentrySdk.Metrics.Increment(eventName, tags: new Dictionary<string, string>
            {
                {"IsSuccess", "false"},
                {"Reason", "仍有课表在使用该时间表。"}
            });
            return;
        }

        var r = (bool?)await DialogHost.Show(FindResource("DeleteTimeLayoutConfirm"), dialogIdentifier: ViewModel.DialogHostId);
        if (r == true)
        {
            SentrySdk.Metrics.Increment(eventName, tags: new Dictionary<string, string>
            {
                {"IsSuccess", "true"}
            });
            MainViewModel.Profile.TimeLayouts.Remove(((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Key);
        }
        else
        {
            SentrySdk.Metrics.Increment(eventName, tags: new Dictionary<string, string>
            {
                {"IsSuccess", "false"},
                {"Reason", "用户取消操作。"}
            });
        }
    }

    private void ButtonAddSubject_OnClick(object sender, RoutedEventArgs e)
    {
        //DataGridSubjects.CancelEdit();
        
        var isCreating = DataGridSubjects.SelectedIndex == MainViewModel.Profile.Subjects.Count;
        
        DataGridSubjects.CancelEdit();
        DataGridSubjects.IsReadOnly = true;
        MainViewModel.Profile.EditingSubjects.Add(new Subject());
        DataGridSubjects.IsReadOnly = false;
        DataGridSubjects.SelectedIndex = MainViewModel.Profile.Subjects.Count - 1;
        //TextBoxSubjectName.Focus();
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.subject.create");
    }

    private async void ButtonSubject_OnClick(object sender, RoutedEventArgs e)
    {
        var r = (bool?)await DialogHost.Show(FindResource("DeleteSubjectConfirm"),dialogIdentifier: ViewModel.DialogHostId);
        if (r == true)
        {
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.subject.remove", tags: new Dictionary<string, string>
            {
                {"IsSuccess", "true"},
            });

            DataGridSubjects.CancelEdit();
            DataGridSubjects.IsReadOnly = true;
            var rm = new List<Subject>();
            foreach (var i in DataGridSubjects.SelectedItems)
            {
                if (i is Subject o)
                {
                    rm.Add(o);
                }
            }
            var s = MainViewModel.Profile.EditingSubjects;
            foreach (var t in rm)
            {
                s.Remove(t);
            }
            DataGridSubjects.IsReadOnly = false;
        }
        else
        {
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.subject.remove", tags: new Dictionary<string, string>
            {
                {"IsSuccess", "false"},
                {"Reason", "用户取消操作。"}
            });
        }
    }

    private void ButtonAddClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        CreateClassPlan();
    }

    private void CreateClassPlan()
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.classPlan.create");
        var newClassPlan = new ClassPlan()
        {
            AssociatedGroup = ProfileService.Profile.SelectedClassPlanGroupId
        };
        MainViewModel.Profile.ClassPlans.Add(Guid.NewGuid().ToString(), newClassPlan);
        ViewModel.SelectedClassPlan = newClassPlan;
        ViewModel.IsClassPlanEditComplete = false;
        OpenDrawer("ClassPlansInfoEditor");
    }

    private void ButtonDebugAddNewClass_OnClick(object sender, RoutedEventArgs e)
    {
        var s = (KeyValuePair<string, ClassPlan>?)ListViewClassPlans.SelectedItem;
        s?.Value.Classes.Add(new ClassInfo());
    }

    private void ButtonClassPlanInfoEdit_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DrawerContent = FindResource("ClassPlansInfoEditor");
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.classPlan.edit");
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
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.classPlan.remove", tags: new Dictionary<string, string>
            {
                {"IsSuccess", "true"}
            });

            var kvp = ((KeyValuePair<string, ClassPlan>)ListViewClassPlans.SelectedItem);
            if (kvp.Value.IsOverlay)
            {
                ProfileService.ClearTempClassPlan();
            }
            else
            {
                MainViewModel.Profile.ClassPlans.Remove(kvp.Key);
            }
        }
        else
        {
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.classPlan.remove", tags: new Dictionary<string, string>
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
        if (ViewModel.DrawerContent == timeLayoutItemEdit && ViewModel.SelectedTimePoint != null)
        {
            // ReSortTimeLayout(ViewModel.SelectedTimePoint);
        }
    }

    private void AddTimePoint(TimeLayoutItem item)
    {
        var timeLayout = ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Value;
        var l = timeLayout.Layouts;
        for (var i = 0; i < l.Count - 1; i++)
        {
            if (l[i].StartSecond.TimeOfDay < item.StartSecond.TimeOfDay)
                continue;
            timeLayout.InsertTimePoint(i, item);
            return;
        }
        timeLayout.InsertTimePoint(l.Count, item);
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
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.classPlan.duplicate");
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
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.timeLayout.duplicate");
    }

    private void ButtonDuplicateSubject_OnClick(object sender, RoutedEventArgs e)
    {
        DataGridSubjects.CancelEdit();
        DataGridSubjects.IsReadOnly = true;
        foreach (var i in DataGridSubjects.SelectedItems)
        {
            var subject = i as Subject;
            var o = CopyObject(subject);
            if (o == null)
            {
                continue;
            }

            MainViewModel.Profile.EditingSubjects.Add(o);
        }
        DataGridSubjects.SelectedItem = MainViewModel.Profile.EditingSubjects.Last();
        DataGridSubjects.IsReadOnly = false;
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.subject.duplicate");
    }

    private void DataGridClassPlans_OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        ViewModel.IsClassPlansEditing = true;
        var hWnd = (HWND)new WindowInteropHelper(this).Handle;
        SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, 
            GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE) & ~NativeWindowHelper.WS_SYSMENU);
    }

    private void DataGridClassPlans_OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        ViewModel.IsClassPlansEditing = false;
        var hWnd = (HWND)new WindowInteropHelper(this).Handle;
        SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE,
            GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE) | NativeWindowHelper.WS_SYSMENU);
    }

    private void ProfileSettingsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        App.GetService<SettingsService>().Settings.TimeLayoutEditorIndex = TabTimeLayoutEditors.SelectedIndex;
        ProfileService.SaveProfile();
        if (!ViewModel.IsClassPlansEditing)
        {
            Hide();
            IsOpened = false;
        }
    }

    private void ButtonTemporaryClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DrawerContent = FindResource("TemporaryClassPlan");
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.drawers.tempClassPlan.open");
    }

    private void ButtonClearTemporaryClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        ProfileService.Profile.TempClassPlanId = null;
    }

    private void ListBoxTempClassPlanSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //MainViewModel.TemporaryClassPlanSetupTime = DateTime.Now;
        ProfileService.Profile.TempClassPlanSetupTime = DateTime.Now;
    }

    private void TabControlSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource.GetType() != typeof(TabControl))
            return;
        var c = ViewModel.SelectedClassPlan;
        c?.RefreshClassesList();
    }

    private void ButtonProfileManage_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.drawers.profileMgmt.open");
        OpenDrawer("ProfileManager");
    }

    private void SnackbarRestartMessage_OnActionClick(object sender, RoutedEventArgs e)
    {
        AppBase.Current.Restart();
    }

    private async void ButtonCreateProfile_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.create");
        ViewModel.CreateProfileName = "";
        var r = await DialogHost.Show(FindResource("CreateProfileDialog"), ViewModel.DialogHostId);
        Debug.WriteLine(r);

        var path = $"./Profiles/{r}.json";
        if (r == null || File.Exists(path))
        {
            return;
        }

        var profile = new Profile();
        var subject = await new StreamReader(Application.GetResourceStream(new Uri("/Assets/default-subjects.json", UriKind.Relative))!.Stream).ReadToEndAsync();
        profile.Subjects = JsonSerializer.Deserialize<Profile>(subject)!.Subjects;
        var json = JsonSerializer.Serialize(profile);
        await File.WriteAllTextAsync(path, json);
        RefreshProfiles();
    }

    private void ButtonOpenProfileFolder_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.openFolder");
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath("./Profiles/"),
            UseShellExecute = true
        });
    }

    private void ButtonRefreshProfiles_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.refresh");
        RefreshProfiles();
    }

    private async void MenuItemRenameProfile_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.rename");
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
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.remove", tags: new Dictionary<string, string>
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
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.remove", tags: new Dictionary<string, string>
            {
                {"IsSuccess", "true"}
            });
            File.Delete(path);
        }
        else
        {
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.remove", tags: new Dictionary<string, string>
            {
                {"Reason", "用户取消操作。"},
                {"IsSuccess", "false"}
            });
        }
        RefreshProfiles();
    }

    private void MenuItemProfileDuplicate_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.duplicate");
        var raw = $"./Profiles/{ViewModel.SelectedProfile}";
        var d = Path.GetFileNameWithoutExtension(ViewModel.SelectedProfile) + " - 副本.json";
        var d1 = $"./Profiles/{d}";
        File.Copy(raw, d1);
        RefreshProfiles();
    }

    public static async void OpenFromFile(string path)
    {
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


    private void ButtonZoomOut_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TimeLineScale > 1.0)
        {
            ViewModel.TimeLineScale -= 0.2;
        }
        ViewModel.TimeLineScale = Math.Round(ViewModel.TimeLineScale, 1);
        TimeLineListControl.ScrollIntoView(TimeLineListControl.SelectedItem);
    }

    private void ButtonZoomIn_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TimeLineScale < 5.0)
        {
            ViewModel.TimeLineScale += 0.2;
        }
        ViewModel.TimeLineScale = Math.Round(ViewModel.TimeLineScale, 1);
        TimeLineListControl.ScrollIntoView(TimeLineListControl.SelectedItem);
    }

    private void ButtonCreateTempOverlayClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        if (ProfileService.Profile.OverlayClassPlanId != null &&
            ProfileService.Profile.ClassPlans.ContainsKey(ProfileService.Profile.OverlayClassPlanId))
        {
            ViewModel.MessageQueue.Enqueue("已存在一个临时层课表，无法创建新的临时层课表。");
        }
        var id = ProfileService.CreateTempClassPlan(((KeyValuePair<string, ClassPlan>)ListViewClassPlans.SelectedItem).Key,
            ViewModel.TempOverlayClassPlanTimeLayoutId);
        if (id != null)
        {
            ListViewClassPlans.SelectedItem = new KeyValuePair<string,ClassPlan>(id, ProfileService.Profile.ClassPlans[id]);
            OpenDrawer("ClassPlansInfoEditor");
        }

        PopupCreateTempOverlayClassPlan.IsOpen = false;
    }

    private void ClassPlanSource_OnFilter(object sender, FilterEventArgs e)
    {
        var cp = (KeyValuePair<string, ClassPlan>)e.Item;
        e.Accepted = !cp.Value.IsOverlay;
    }

    private void ButtonClearTempOverlay_OnClick(object sender, RoutedEventArgs e)
    {
        ProfileService.ClearTempClassPlan();
    }

    private void ButtonConvertToStdClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        ProfileService.ConvertToStdClassPlan();
    }

    private void ButtonTimeLayoutEditScrollToContent_OnClick(object sender, RoutedEventArgs e)
    {
        TimeLayoutEditScrollToContent();
    }

    private void TimeLayoutEditScrollToContent()
    {
        var timeLayoutItems = ((KeyValuePair<string, TimeLayout>?)ListViewTimeLayouts.SelectedItem)?.Value.Layouts;
        var tpr = ViewModel.SelectedTimePoint ?? (timeLayoutItems is { Count: > 0 } ? timeLayoutItems?[0] : null);
        if (tpr == null)
        {
            return;
        }

        ViewModel.SelectedTimePoint = null;
        ViewModel.SelectedTimePoint = tpr;
        TimeLineListControl.ScrollIntoView(tpr);
        ListViewTimePoints.ScrollIntoView(tpr);
    }

    private void ButtonImportFromFile_OnClick(object sender, RoutedEventArgs e)
    {
        if (ManagementService.Policy.DisableProfileClassPlanEditing ||
            ManagementService.Policy.DisableProfileTimeLayoutEditing || ManagementService.Policy.DisableProfileEditing)
        {
            ViewModel.MessageQueue.Enqueue($"此功能已被您的组织禁用。");
            return;
        }
        var eiw = App.GetService<ExcelImportWindow>();
        eiw.Show();
    }   

    private void ProfileSettingsWindow_OnDrop(object sender, DragEventArgs e)
    {
        ViewModel.IsDragEntering = false;
        if (e.Data.GetData(DataFormats.FileDrop) is not Array data)
            return;
        var filename = data.GetValue(0)?.ToString();
        if (filename == null)
            return;
        Debug.WriteLine(filename);
        if (ManagementService.Policy.DisableProfileClassPlanEditing ||
            ManagementService.Policy.DisableProfileTimeLayoutEditing || ManagementService.Policy.DisableProfileEditing)
        {
            ViewModel.MessageQueue.Enqueue($"此功能已被您的组织禁用。");
            return;
        }
        if (Path.GetExtension(filename) != ".xlsx")
        {
            ViewModel.MessageQueue.Enqueue($"不支持的文件：{filename}");
            return;
        }
        var eiw = App.GetService<ExcelImportWindow>();
        eiw.ExcelSourcePath = filename;
        eiw.Show();
    }

    private void ProfileSettingsWindow_OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {

            ViewModel.IsDragEntering = true;
            e.Effects = DragDropEffects.Link;
        }
        else
        {
            ViewModel.IsDragEntering = false;
            e.Effects = DragDropEffects.None;
        }
    }

    private void ListViewTimeLayouts_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //TimeLayoutEditScrollToContent();
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TimeLayoutEditScrollToContent();
    }

    private void GlobalUpdated(object sender, RoutedEventArgs e)
    {
        HangService.AssumeHang();
    }

    private void ButtonOverwriteClasses_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTimePoint == null)
            return;
        MainViewModel.Profile.OverwriteAllClassPlanSubject(
            ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Key,
            ViewModel.SelectedTimePoint,
            ViewModel.SelectedTimePoint.DefaultClassId);
    }

    private void ProfileSettingsWindow_OnDragLeave(object sender, DragEventArgs e)
    {
        ViewModel.IsDragEntering = false;
    }

    private void ButtonHelp_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<MainWindow>().OpenHelpsWindow();
        App.GetService<HelpsWindow>().InitDocumentName = "档案设置";
        App.GetService<HelpsWindow>().ViewModel.SelectedDocumentName = "档案设置";
    }

    private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
    {
        ProfileService.SaveProfile();
        ViewModel.MessageQueue.Enqueue($"已保存到{ProfileService.CurrentProfilePath}。");
    }

    private void ButtonBeginCreateTempOverlayClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        if (ProfileService.Profile.OverlayClassPlanId != null &&
            ProfileService.Profile.ClassPlans.ContainsKey(ProfileService.Profile.OverlayClassPlanId))
        {
            ViewModel.MessageQueue.Enqueue("已存在一个临时层课表，无法创建新的临时层课表。");
            return;
        }
        ViewModel.TempOverlayClassPlanTimeLayoutId =
            ((KeyValuePair<string, ClassPlan>)ListViewClassPlans.SelectedItem).Value.TimeLayoutId;
        PopupCreateTempOverlayClassPlan.IsOpen = true;
    }

    private void ComboBoxClassPlanGroup_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var source = FindResource("ClassPlansViewSource") as CollectionViewSource;
        if (source == null)
        {
            return;
        }
    }

    private void ButtonClassPlansGroup_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("ClassPlanGroups");
    }

    private void ClassPlanGroupsSource_OnFilter(object sender, FilterEventArgs e)
    {

    }

    private void ButtonNewClassPlanGroups_OnClick(object sender, RoutedEventArgs e)
    {
        ProfileService.Profile.ClassPlanGroups.Add(Guid.NewGuid().ToString(), new());
    }

    private void ButtonRefreshClassPlans_OnClick(object sender, RoutedEventArgs e)
    {
        var source = FindResource("ClassPlansViewSource") as CollectionViewSource;
        source?.View?.Refresh();
    }

    private void ButtonClearTempClassPlanGroup_OnClick(object sender, RoutedEventArgs e)
    {
        ProfileService.ClearTempClassPlanGroup();
    }

    public void Open()
    {
        if (!IsOpened)
        {
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.open");
            IsOpened = true;
            Show();
        }
        else
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
        }
    }

    private void EventSetterSubjectSelector_OnClick(object sender, object args)
    {
        if (!MainViewModel.Settings.IsProfileEditorClassInfoSubjectAutoMoveNextEnabled)
            return;
        if (ViewModel.SelectedClassIndex + 1 >= ViewModel.SelectedClassPlan.Classes.Count)
        {
            ViewModel.IsClassPlanEditComplete = true;
            return;
        }
        ViewModel.SelectedClassIndex++;
        ViewModel.IsClassPlanEditComplete = false;
        DataGridClassPlans.ScrollIntoView(DataGridClassPlans.SelectedItem);
    }

    private void ButtonCloseCompleteTip_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsClassPlanEditComplete = false;
    }

    private void ButtonAddClassPlanFromCompletedTip_OnClick(object sender, RoutedEventArgs e)
    {
        CreateClassPlan();
    }
}
