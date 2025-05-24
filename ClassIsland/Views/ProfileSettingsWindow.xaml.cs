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
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Converters;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Models;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Extensions;
using ClassIsland.Shared.Models.Action;
using ClassIsland.ViewModels;
using CommunityToolkit.Mvvm.Input;
using CsesSharp;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sentry;
using Application = System.Windows.Application;
using ClassPlanDictionaryValueAccessConverter = ClassIsland.Core.Converters.ClassPlanDictionaryValueAccessConverter;
using CommonDialog = ClassIsland.Core.Controls.CommonDialog.CommonDialog;
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

    public IExactTimeService ExactTimeService { get; } = App.GetService<IExactTimeService>();

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
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        ViewModel.PropertyChanging += ViewModelOnPropertyChanging;
    }

    private void ViewModelOnPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
    }

    private void SelectedTimePointOnPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (e.PropertyName != nameof(ViewModel.SelectedTimePoint.TimeType)) 
            return;
        NotifyTimePointChanged(true);
    }

    private void NotifyTimePointChanged(bool isRemove)
    {
        if (ViewModel.SelectedTimePoint == null)
            return;
        var timeLayout = ((KeyValuePair<string, TimeLayout>)ListViewTimeLayouts.SelectedItem).Value;
        var index = timeLayout.Layouts.IndexOf(ViewModel.SelectedTimePoint);
        if (index == -1)
            return;
        if (!isRemove){
            timeLayout.NotifyTimeLayoutItemAdded(index, ViewModel.SelectedTimePoint);
        }
        else
        {
            timeLayout.NotifyTimeLayoutItemRemoved(index, ViewModel.SelectedTimePoint);
        }
    }

    private void SelectedTimePointOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ViewModel.SelectedTimePoint.TimeType)) 
            return;
        NotifyTimePointChanged(false);
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedTimePoint))
        {
            if (ViewModel.PreviousTrackedTimeLayoutItem != null)
            {
                ViewModel.PreviousTrackedTimeLayoutItem.PropertyChanged -= SelectedTimePointOnPropertyChanged;
                ViewModel.PreviousTrackedTimeLayoutItem.PropertyChanging -= SelectedTimePointOnPropertyChanging;
            }

            if (ViewModel.SelectedTimePoint != null)
            {
                ViewModel.SelectedTimePoint.PropertyChanged += SelectedTimePointOnPropertyChanged;
                ViewModel.SelectedTimePoint.PropertyChanging += SelectedTimePointOnPropertyChanging;
                ViewModel.PreviousTrackedTimeLayoutItem = ViewModel.SelectedTimePoint;
            }
        }

        if (e.PropertyName == nameof(ViewModel.ScheduleCalendarSelectedDate) && ScheduleAdjustmentTabControl.SelectedIndex == 0)
        {
            RefreshWeekScheduleRows();
        }
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
            (from i in Directory.GetFiles(Services.ProfileService.ProfilePath)
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
        var baseSec    = (timeType is 0 or 1 ? selected?.EndSecond : selected?.StartSecond) ?? DateTime.Today + new TimeSpan(7, 30, 0);
        var settings   = App.GetService<SettingsService>().Settings;
        var lastTime   = TimeSpan.FromMinutes(timeType switch
        {
            0 => settings.DefaultOnClassTimePointMinutes,  // 上课
            1 => settings.DefaultBreakingTimePointMinutes, // 课间休息
            2 => 0,  // 分割线
            3 => 0,  // 行动
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
            if (timeType != 2 && timeType != 3 && index < timeLayout.Layouts.Count - 1)
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

            if (timeType == 3)
            {
                baseSec = selected.EndSecond;
                if ((from i in timeLayout.Layouts where i.TimeType == 3 select i.StartSecond).ToList().Contains(baseSec))
                {
                    ViewModel.MessageQueue.Enqueue("这里已经存在一个行动。");
                    return;
                }
            }
        }
        var newItem = new TimeLayoutItem()
        {
            TimeType = timeType,
            StartSecond = baseSec,
            EndSecond = baseSec + lastTime,
            ActionSet = timeType == 3 ? new ActionSet() : null
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
        timeLayout.SortCompleted();
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
        //UpdateTimeLayout();
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

    private void ButtonRefreshTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        UpdateTimeLayout();
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

    private void Subject_OnPaste(object? sender, ExecutedRoutedEventArgs e)
    {
        if (ManagementService.Policy.DisableProfileSubjectsEditing) return;

        foreach (var i in Clipboard.GetText().Split("\n").Select(i => i.Replace("\r", "")).Where(i => !string.IsNullOrWhiteSpace(i)))
        {
            if (DataGridSubjects.SelectedIndex == MainViewModel.Profile.EditingSubjects.Count)
                MainViewModel.Profile.EditingSubjects.Add(new Subject { Name = i });
            else
                MainViewModel.Profile.EditingSubjects.Insert(DataGridSubjects.SelectedIndex + 1, new Subject { Name = i });
            DataGridSubjects.SelectedIndex += 1;
        }
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
            MainViewModel.Profile.ClassPlans.Remove(kvp.Key);
            foreach (var (key, _) in MainViewModel.Profile.OrderedSchedules.Where(x => x.Value.ClassPlanId == kvp.Key).ToList())
            {
                MainViewModel.Profile.OrderedSchedules.Remove(key);
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
            if (l[i].StartSecond.TimeOfDay <= item.StartSecond.TimeOfDay)
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
        //MainViewModel.TemporaryClassPlanSetupTime = ExactTimeService.GetCurrentLocalDateTime();
        ProfileService.Profile.TempClassPlanSetupTime = ExactTimeService.GetCurrentLocalDateTime();
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

        var path = Path.Combine(Services.ProfileService.ProfilePath, $"{r}.json");
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
            FileName = Path.GetFullPath(Services.ProfileService.ProfilePath),
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

        var raw = Path.Combine(Services.ProfileService.ProfilePath, $"{ViewModel.SelectedProfile}");
        var path = Path.Combine(Services.ProfileService.ProfilePath, $"{r}.json");
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
        var path = Path.Combine(Services.ProfileService.ProfilePath, $"{ViewModel.SelectedProfile}");
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
        var raw = Path.Combine(Services.ProfileService.ProfilePath, $"{ViewModel.SelectedProfile}");
        var d = Path.GetFileNameWithoutExtension(ViewModel.SelectedProfile) + " - 副本.json";
        var d1 = Path.Combine(Services.ProfileService.ProfilePath, $"{d}");
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
        var id = ProfileService.CreateTempClassPlan(((KeyValuePair<string, ClassPlan>)ListViewClassPlans.SelectedItem).Key,
            ViewModel.TempOverlayClassPlanTimeLayoutId,
            ViewModel.OverlayEnableDateTime);
        if (id != null)
        {
            ListViewClassPlans.SelectedItem = new KeyValuePair<string,ClassPlan>(id, ProfileService.Profile.ClassPlans[id]);
            OpenDrawer("ClassPlansInfoEditor");
        }
        else
        {
            ViewModel.MessageQueue.Enqueue("在这一天已存在一个临时层课表，无法创建新的临时层课表。");
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

        ViewModel.IsProfileImportMenuOpened = true;
        //var eiw = App.GetService<ExcelImportWindow>();
        //eiw.Show();
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

    private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
    {
        SaveProfile();
    }

    [RelayCommand]
    private void SaveProfile()
    {
        ProfileService.SaveProfile();
        ViewModel.MessageQueue.Enqueue($"已保存到{ProfileService.CurrentProfilePath}。");
    }

    private void ButtonBeginCreateTempOverlayClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OverlayEnableDateTime = ExactTimeService.GetCurrentLocalDateTime().Date;
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

    public async void Open()
    {
        if (!IsOpened)
        {
            if (!await ManagementService.AuthorizeByLevel(ManagementService.CredentialConfig.EditProfileAuthorizeLevel))
            {
                return;
            }
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

    private void ButtonClassPlanDetails_OnClick(object sender, RoutedEventArgs e)
    {
        var details = App.GetService<ClassPlanDetailsWindow>();
        details.ViewModel.ClassPlan = ViewModel.SelectedClassPlan;
        details.Owner = this;
        details.ShowDialog();
    }

    private void MultiWeekRotation_OnLoaded(object sender, RoutedEventArgs e)
    {
        // LessonsService.RefreshMultiWeekRotation();
    }

    private void MultiWeekRotation_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void ButtonOpenWeekOffsetSettings_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsWeekOffsetSettingsOpen = true;
    }

    private void ButtonWeekOffsetSettingsButtons_OnClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Button)
        {
            return;
        }
        ViewModel.IsWeekOffsetSettingsOpen = false;
    }

    private void ButtonAddActionTimePoint_OnClick(object sender, RoutedEventArgs e)
    {
        AddTimeLayoutItem(3);
    }

    private void ButtonDebugTriggerAction_OnClick(object sender, RoutedEventArgs e)
    {
        var action = ViewModel.SelectedTimePoint?.ActionSet;
        if (action == null)
        {
            return;
        }
        IAppHost.GetService<IActionService>().Invoke(action);
    }

    private async void ButtonUnTrustedProfile_OnClick(object sender, RoutedEventArgs e)
    {
        var r = await DialogHost.Show(FindResource("ProfileTrustWarning"), ViewModel.DialogHostId);
        if (r as bool? != true)
        {
            return;
        }
        ProfileService.TrustCurrentProfile();
    }

    private void MenuItemImportFromExcel_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsProfileImportMenuOpened = false;
        var eiw = App.GetService<ExcelImportWindow>();
        eiw.Show();
    }

    private async void MenuItemImportFromCses_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsProfileImportMenuOpened = false;
        var r = await DialogHost.Show(new CsesImportControl(), ViewModel.DialogHostId);
        if (r as bool? == true)
        {
            ViewModel.MessageQueue.Enqueue("成功导入了 CSES 课表。");
            RefreshProfiles();
        }
    }

    private void MenuItemImports_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsProfileImportMenuOpened = false;
    }

    private void MenuItemExportCses_OnClick(object sender, RoutedEventArgs e)
    {
        var warnings = new List<string>();
        foreach (var i in ProfileService.Profile.ClassPlans)
        {
            if (i.Value.TimeRule.WeekCountDivTotal > 2 || i.Value.TimeRule.WeekCountDiv > 2)
            {
                warnings.Add($"课程表 {i.Value.Name}：无法导出包含 2 周以上轮换的课表。");
            }
            if (i.Value.TimeLayout == null)
            {
                warnings.Add($"课程表 {i.Value.Name}：无法导出使用无效时间表的课表。");
            }
            if (i.Value.IsEnabled == false)
            {
                warnings.Add($"课程表 {i.Value.Name}：无法导出不默认启用的课表。");
            }
        }

        if (warnings.Count > 0)
        {
            var r = new CommonDialogBuilder()
                .SetIconKind(CommonDialogIconKind.Hint)
                .SetContent("兼容性警告：以下课表无法导出到 CSES 格式：\n" + string.Join('\n', warnings) + "\n\n是否继续导出？")
                .AddCancelAction()
                .AddAction("继续", PackIconKind.Check, true)
                .ShowDialog(this);
            if (r == 0)
            {
                return;
            }
        }

        var dialog = new SaveFileDialog()
        {
            Filter = "CSES 课表文件(*.yml, *.yaml)|*.yml;*.yaml"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var csesProfile = ProfileService.Profile.ToCsesObject();
            CsesLoader.SaveToYamlFile(csesProfile, dialog.FileName);
            Process.Start(new ProcessStartInfo()
            {
                FileName = Path.GetDirectoryName(Path.GetFullPath(dialog.FileName)),
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            IAppHost.GetService<ILogger<ProfileSettingsWindow>>().LogError(exception, "无法导出到 CSES 课表");
            CommonDialog.ShowError($"无法导出到 CSES 课表：{exception.Message}");
        }
    }

    private void ButtonScheduleCalendarPrevMonth_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void RefreshWeekScheduleRows()
    {
        var selectedDate = ViewModel.ScheduleCalendarSelectedDate.Date;
        var baseDate = selectedDate.AddDays(-(int)selectedDate.DayOfWeek);
        ViewModel.ScheduleWeekViewBaseDate = baseDate;
        List<ClassPlan?> classPlans = [];
        ViewModel.WeekClassPlanRows.Clear();
        var maxClasses = 0;
        for (var i = 0; i < 7; i++)
        {
            var classPlan = LessonsService.GetClassPlanByDate(baseDate.AddDays(i));
            maxClasses = Math.Max(maxClasses, classPlan?.Classes.Count ?? 0);
            classPlans.Add(classPlan);
        }

        for (var i = 0; i < maxClasses; i++)
        {
            var row = new WeekClassPlanRow()
            {
                Sunday = TryGetClassInfo(classPlans[0], i),
                Monday = TryGetClassInfo(classPlans[1], i),
                Tuesday = TryGetClassInfo(classPlans[2], i),
                Wednesday = TryGetClassInfo(classPlans[3], i),
                Thursday = TryGetClassInfo(classPlans[4], i),
                Friday = TryGetClassInfo(classPlans[5], i),
                Saturday = TryGetClassInfo(classPlans[6], i),
            };
            ViewModel.WeekClassPlanRows.Add(row);
        }

        ViewModel.DataGridWeekRowsWeekIndex =
            (int)Math.Ceiling((baseDate.AddDays(6) - MainViewModel.Settings.SingleWeekStartTime).TotalDays / 7);

        return;

        ClassInfo? TryGetClassInfo(ClassPlan? classPlan, int index)
        {
            if (classPlan != null && classPlan.Classes.Count > index)
            {
                return classPlan.Classes[index];
            }

            return null;
        }
    }
    private void ButtonRefreshScheduleAdjustmentView_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshWeekScheduleRows();
        ScheduleCalendarControl.UpdateSchedule();
    }

    private void DataGridWeekSchedule_OnPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
    {
    }

    private void DataGridWeekSchedule_OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Row.Item is WeekClassPlanRow row &&
            GetClassInfoFromRow(row, e.Column.DisplayIndex) == null)
        {
            e.Cancel = true;
        }
    }

    private ClassInfo? GetClassInfoFromRow(WeekClassPlanRow row, int index)
    {
        return index switch
        {
            0 => row.Sunday,
            1 => row.Monday,
            2 => row.Tuesday,
            3 => row.Wednesday,
            4 => row.Thursday,
            5 => row.Friday,
            6 => row.Saturday,
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
        };
    }

    private void ButtonSwapSchedule_OnClick(object sender, RoutedEventArgs e)
    {
        var cell = DataGridWeekSchedule.SelectedCells.FirstOrDefault();
        if (cell.Item is not WeekClassPlanRow row)
        {
            ViewModel.MessageQueue.Enqueue("请先选择要交换的课程。");
            return;
        }
        var date = ViewModel.ScheduleWeekViewBaseDate.AddDays(cell.Column.DisplayIndex);
        var index = ViewModel.WeekClassPlanRows.IndexOf(row);
        if (GetClassInfoFromRow(row, cell.Column.DisplayIndex) == null)
        {
            ViewModel.MessageQueue.Enqueue("选择课程区域无效。");
            return;
        }
        ViewModel.ClassSwapStartPosition = new ScheduleClassPosition(date, index);
        ViewModel.IsInScheduleSwappingMode = true;
    }

    private void ButtonNextWeek_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ScheduleCalendarSelectedDate += TimeSpan.FromDays(7);
        RefreshWeekScheduleRows();
    }

    private void ButtonPreviousWeek_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ScheduleCalendarSelectedDate -= TimeSpan.FromDays(7);
        RefreshWeekScheduleRows();
    }

    private void ButtonSwapScheduleComplete_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsInScheduleSwappingMode = false;
        var cell = DataGridWeekSchedule.SelectedCells.FirstOrDefault();
        if (cell.Item is not WeekClassPlanRow row)
        {
            return;
        }
        var date = ViewModel.ScheduleWeekViewBaseDate.AddDays(cell.Column.DisplayIndex);
        var index = ViewModel.WeekClassPlanRows.IndexOf(row);
        if (GetClassInfoFromRow(row, cell.Column.DisplayIndex) == null)
        {
            ViewModel.MessageQueue.Enqueue("选择课程区域无效。");
            return;
        }
        ViewModel.ClassSwapEndPosition = new ScheduleClassPosition(date, index);

        var startOverlay = GetTargetClassPlan(ViewModel.ClassSwapStartPosition.Date, ViewModel.IsTempSwapMode, out _);
        var endOverlay = GetTargetClassPlan(ViewModel.ClassSwapEndPosition.Date, ViewModel.IsTempSwapMode, out _);
        if (startOverlay == null || endOverlay == null ||
            endOverlay.Classes.Count <= ViewModel.ClassSwapEndPosition.Index ||
            startOverlay.Classes.Count <= ViewModel.ClassSwapStartPosition.Index)
        {
            return;
        }

        (startOverlay.Classes[ViewModel.ClassSwapStartPosition.Index].SubjectId, endOverlay.Classes[ViewModel.ClassSwapEndPosition.Index].SubjectId) = (endOverlay.Classes[ViewModel.ClassSwapEndPosition.Index].SubjectId, startOverlay.Classes[ViewModel.ClassSwapStartPosition.Index].SubjectId);
        if (ViewModel.IsTempSwapMode)
        {
            startOverlay.Classes[ViewModel.ClassSwapStartPosition.Index].IsChangedClass = true;
            endOverlay.Classes[ViewModel.ClassSwapEndPosition.Index].IsChangedClass = true;
        }

        RefreshWeekScheduleRows();
        ScheduleCalendarControl.UpdateSchedule();
    }

    private ClassPlan? GetTargetClassPlan(DateTime dateTime, bool overlay, out string? targetGuid)
    {
        targetGuid = null;
        var baseClassPlan = LessonsService.GetClassPlanByDate(dateTime, out var baseGuid);
        if (baseClassPlan == null || baseGuid == null)
        {
            return null;
        }

        if (!overlay || baseClassPlan.IsOverlay)
        {
            targetGuid = baseGuid;
            return baseClassPlan;
        }

        var orderedClassPlanId = ProfileService.Profile.OrderedSchedules[dateTime]?.ClassPlanId;
        if (orderedClassPlanId != null
            && ProfileService.Profile.ClassPlans.TryGetValue(orderedClassPlanId, out var classPlan)
            && classPlan.IsOverlay)
        {
            targetGuid = baseGuid;
            return baseClassPlan;
        }

        targetGuid =
            ProfileService.CreateTempClassPlan(baseGuid, enableDateTime: dateTime);
        return targetGuid == null ? null : ProfileService.Profile.ClassPlans[targetGuid];
    }

    private void HyperlinkNavigateTimeLayoutPage_OnClick(object sender, RoutedEventArgs e)
    {
        RootTabControl.SelectedIndex = 1;
    }

    private void ButtonCancelClassSwap_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsInScheduleSwappingMode = false;
    }

    private void ButtonEditClassInfoTemp_OnClick(object sender, RoutedEventArgs e)
    {
        var cell = DataGridWeekSchedule.SelectedCells.FirstOrDefault();
        if (cell.Item is not WeekClassPlanRow row)
        {
            ViewModel.MessageQueue.Enqueue("请先选择要修改的课程。");
            return;
        }
        if (GetClassInfoFromRow(row, cell.Column.DisplayIndex) == null)
        {
            ViewModel.MessageQueue.Enqueue("选择课程区域无效。");
            return;
        }

        ViewModel.TargetSubjectIndex = "";
        ViewModel.IsClassPlanTempEditPopupOpen = true;
    }

    private void ButtonEditClassInfoTempConfirm_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsClassPlanTempEditPopupOpen = false;
        var cell = DataGridWeekSchedule.SelectedCells.FirstOrDefault();
        if (cell.Item is not WeekClassPlanRow row)
        {
            return;
        }
        var date = ViewModel.ScheduleWeekViewBaseDate.AddDays(cell.Column.DisplayIndex);
        var index = ViewModel.WeekClassPlanRows.IndexOf(row);

        var targetClassPlan = GetTargetClassPlan(date, ViewModel.IsTempSwapMode, out var guid);
        if (targetClassPlan == null || targetClassPlan.Classes.Count <= index)
        {
            return;
        }

        targetClassPlan.Classes[index].SubjectId = ViewModel.TargetSubjectIndex;
        if (ViewModel.IsTempSwapMode)
        {
            targetClassPlan.Classes[index].IsChangedClass = true;
        }
        RefreshWeekScheduleRows();
        ScheduleCalendarControl.UpdateSchedule();
    }

    private void RootTabControlNavigator_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RootTabControl.SelectedIndex == 3)
        {
            RefreshWeekScheduleRows();
        }
    }

    private void ButtonHideSellingAnnouncementBanner_OnClick(object sender, RoutedEventArgs e)
    {
        MainViewModel.Settings.ShowSellingAnnouncement = false;
    }

    private void MenuItemExportExcel_OnClick(object sender, RoutedEventArgs e)
    {
        var win = IAppHost.GetService<ExcelExportWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void ScheduleAdjustmentTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ScheduleAdjustmentTabControl.SelectedIndex != 0)
        {
            return;
        }
        RefreshWeekScheduleRows();
    }
}
