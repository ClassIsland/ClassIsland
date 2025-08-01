using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.VisualTree;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Action;
using ClassIsland.Core.Models.UI;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Action;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Sentry;

namespace ClassIsland.Views;

public partial class ProfileSettingsWindow : MyWindow
{
    private bool _isOpen = false;

    public ProfileSettingsViewModel ViewModel { get; } = IAppHost.GetService<ProfileSettingsViewModel>();

    private ILogger<ProfileSettingsWindow> Logger => ViewModel.Logger;
    public static ICommand RemoveSelectedTimeLayoutItemCommand { get; } = new RoutedCommand(nameof(RemoveSelectedTimeLayoutItemCommand));

    public ProfileSettingsWindow()
    {
        DataContext = this;
        InitializeComponent();
    }

    #region Misc

    public void OpenDrawer(string key)
    {
        ViewModel.IsDrawerOpen = true;
        if (this.FindResource(key) is { } o)
        {
            ViewModel.DrawerContent = o;
        }
    }

    public async void Open()
    {
        if (!_isOpen)
        {
            if (!await ViewModel.ManagementService.AuthorizeByLevel(ViewModel.ManagementService.CredentialConfig
                    .EditProfileAuthorizeLevel))
            {
                return;
            }

            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.open");
            _isOpen = true;
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

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;
        _isOpen = false;
        Hide();
    }
    
    private void MasterTabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (MasterTabControl?.SelectedIndex == 3)
        {
            RefreshWeekScheduleRows();
        }
    }

    #endregion

    #region Profile

    private void RefreshProfiles()
    {
        ViewModel.Profiles = new ObservableCollection<string>
        (from i in Directory.GetFiles(Services.ProfileService.ProfilePath)
            where i.EndsWith(".json")
            select Path.GetFileName(i));
    }

    private void ButtonSaveProfile_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.ProfileService.SaveProfile();
            this.ShowToast(new ToastMessage($"已保存到 {ViewModel.ProfileService.CurrentProfilePath}。")
            {
                Severity = InfoBarSeverity.Success
            });
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法保存档案");
            this.ShowErrorToast("无法保存档案", exception);
        }
    }

    private async void ButtonCreateProfile_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.create");
        ViewModel.CreateProfileName = "";
        var textBox = new TextBox();
        var r = await new ContentDialog()
        {
            Title = "新建档案",
            Content = new Field()
            {
                Content = textBox,
                Label = "档案名称",
                Suffix = ".json"
            },
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "新建",
            SecondaryButtonText = "取消"
        }.ShowAsync();

        var path = Path.Combine(Services.ProfileService.ProfilePath, $"{textBox.Text}.json");
        if (r != ContentDialogResult.Primary || File.Exists(path))
        {
            return;
        }

        var profile = new Profile();
        var subject =
            await new StreamReader(AssetLoader.Open(new Uri("avares://ClassIsland/Assets/default-subjects.json",
                UriKind.Absolute))).ReadToEndAsync();
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
        var textBox = new TextBox()
        {
            Text = ViewModel.RenameProfileName
        };
        var r = await new ContentDialog()
        {
            Title = "重命名档案",
            Content = new Field()
            {
                Content = textBox,
                Label = "档案名称",
                Suffix = ".json"
            },
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "重命名",
            SecondaryButtonText = "取消"
        }.ShowAsync();

        var raw = Path.Combine(Services.ProfileService.ProfilePath, $"{ViewModel.SelectedProfile}");
        var path = Path.Combine(Services.ProfileService.ProfilePath, $"{textBox.Text}.json");
        if (r != ContentDialogResult.Primary || !File.Exists(raw))
        {
            return;
        }

        if (File.Exists(path))
        {
            this.ShowToast(new ToastMessage()
            {
                Message = "无法重命名档案，因为已存在一个相同名称的档案。",
                Severity = InfoBarSeverity.Warning
            });
            return;
        }

        File.Move(raw, path);
        if (ViewModel.ProfileService.CurrentProfilePath == Path.GetFileName(raw))
        {
            ViewModel.ProfileService.CurrentProfilePath = Path.GetFileName(path);
            ViewModel.SettingsService.Settings.SelectedProfile = Path.GetFileName(path);
        }

        RefreshProfiles();
    }

    private async void MenuItemDeleteProfile_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteConfirmField = "";
        var path = Path.Combine(Services.ProfileService.ProfilePath, $"{ViewModel.SelectedProfile}");
        if (ViewModel.SelectedProfile == ViewModel.ProfileService.CurrentProfilePath ||
            ViewModel.SelectedProfile == ViewModel.SettingsService.Settings.SelectedProfile)
        {
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.remove",
                tags: new Dictionary<string, string>
                {
                    { "Reason", "正在删除已加载或将要加载的档案。" },
                    { "IsSuccess", "false" }
                });
            this.ShowToast(new ToastMessage("无法删除已加载或将要加载的档案。")
            {
                Severity = InfoBarSeverity.Warning
            });
            return;
        }

        var textBox = new TextBox();
        var r = await new ContentDialog()
        {
            Title = "删除档案",
            Content = $"您确定要删除档案 {ViewModel.ProfileService.CurrentProfilePath} 吗？此操作无法撤销，档案内的课表、时间表、科目等信息都将被删除！",
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "删除",
            SecondaryButtonText = "取消"
        }.ShowAsync();

        if (r == ContentDialogResult.Primary)
        {
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.remove",
                tags: new Dictionary<string, string>
                {
                    { "IsSuccess", "true" }
                });
            File.Delete(path);
        }
        else
        {
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.remove",
                tags: new Dictionary<string, string>
                {
                    { "Reason", "用户取消操作。" },
                    { "IsSuccess", "false" }
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

    private void ButtonOpenProfileManager_OnClick(object? sender, RoutedEventArgs e)
    {
        RefreshProfiles();
        OpenDrawer("ProfileManager");
    }

    [RelayCommand]
    private void ProfileSelectionRadioButtonToggled(string name)
    {
        ViewModel.SettingsService.Settings.SelectedProfile = name;
        var action = new Button()
        {
            Content = "立即重启"
        };
        action.Click += (sender, args) => AppBase.Current.Restart();
        this.ShowToast(new ToastMessage()
        {
            Title = "需要重启",
            Message = "切换档案需要重启应用以生效。",
            AutoClose = false,
            ActionContent = action
        });
    }

    #endregion

    #region TempClassPlan
    
    private void ButtonOpenTempClassPlans_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenDrawer("TemporaryClassPlan");
    }

    private void ButtonClearTempOverlay_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ProfileService.ClearTempClassPlan();
    }
    
    private void ButtonClearTemporaryClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ProfileService.Profile.TempClassPlanId = null;
    }
    
    private void ListBoxTempClassPlanSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.ProfileService.Profile.TempClassPlanSetupTime = ViewModel.ExactTimeService.GetCurrentLocalDateTime();
    }

    #endregion

    #region ClassPlanGroups
    
    private void ButtonClassPlansGroup_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("ClassPlanGroups");
    }

    private void ButtonClearTempClassPlanGroup_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ProfileService.ClearTempClassPlanGroup();
    }
    
    private void ButtonNewClassPlanGroups_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ProfileService.Profile.ClassPlanGroups.Add(Guid.NewGuid(), new ClassPlanGroup());
    }

    #endregion

    #region ClassPlans

    private void ButtonOpenClassPlanDetailsWindow_OnClick(object? sender, RoutedEventArgs e)
    {
        var details = App.GetService<ClassPlanDetailsWindow>();
        if (ViewModel.SelectedClassPlan == null)
        {
            return;
        }
        details.ViewModel.ClassPlan = ViewModel.SelectedClassPlan;
        _ = details.ShowDialog(this);
    }
    
    private void UpdateClassPlanInfoEditorTimeLayoutComboBox()
    {
        if (ViewModel.SelectedClassPlan?.TimeLayout == null)
        {
            ViewModel.ClassPlanInfoSelectedTimeLayoutKvp = null;
        }
        else
        {
            var kvp = ViewModel.TimeLayouts.List.FirstOrDefault(x => x.Key == ViewModel.SelectedClassPlan.TimeLayoutId);
            ViewModel.ClassPlanInfoSelectedTimeLayoutKvp = kvp;
        }
    }
    
    private void SelectingItemsControlClassPlans_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ViewModel.CurrentClassPlanEditDoneToast?.Close();
    }
    
    private void ButtonCreateTempOverlayClassPlan_OnClick(object? sender, RoutedEventArgs e)
    {
        var key = ViewModel.ProfileService.Profile.ClassPlans
            .FirstOrDefault(x => x.Value == ViewModel.SelectedClassPlan).Key;
        var id = ViewModel.ProfileService.CreateTempClassPlan(key,
            ViewModel.TempOverlayClassPlanTimeLayoutId,
            ViewModel.OverlayEnableDateTime);
        if (id is { } guid)
        {
            ViewModel.SelectedClassPlan = ViewModel.ProfileService.Profile.ClassPlans[guid];
            UpdateClassPlanInfoEditorTimeLayoutComboBox();
            OpenDrawer("ClassPlansInfoEditor");
            FlyoutHelper.CloseAncestorFlyout(sender);
        }
        else
        {
            this.ShowToast(new ToastMessage("在这一天已存在一个临时层课表，无法创建新的临时层课表。")
            {
                Severity = InfoBarSeverity.Warning,
                Duration = TimeSpan.FromSeconds(5)
            });
        }

    }

    private void ButtonOpenClassPlanDetails_OnClick(object? sender, RoutedEventArgs e)
    {
        UpdateClassPlanInfoEditorTimeLayoutComboBox();
        OpenDrawer("ClassPlansInfoEditor");
    }

    private void ButtonAddClassPlan_OnClick(object? sender, RoutedEventArgs e)
    {
        CreateClassPlan();
    }

    private void CreateClassPlan()
    {
        var newClassPlan = new ClassPlan();
        ViewModel.ProfileService.Profile.ClassPlans.Add(Guid.NewGuid(), newClassPlan);
        ViewModel.SelectedClassPlan = newClassPlan;
        UpdateClassPlanInfoEditorTimeLayoutComboBox();
        OpenDrawer("ClassPlansInfoEditor");
    }

    private void ButtonDeleteSelectedClassPlan_OnClick(object? sender, RoutedEventArgs e)
    {
        var k = ViewModel.ProfileService.Profile.ClassPlans
            .FirstOrDefault(x => x.Value == ViewModel.SelectedClassPlan).Key;
        ViewModel.ProfileService.Profile.ClassPlans.Remove(k);
        foreach (var (key, _) in ViewModel.ProfileService.Profile.OrderedSchedules.Where(x => x.Value.ClassPlanId == k).ToList())
        {
            ViewModel.ProfileService.Profile.OrderedSchedules.Remove(key);
        }
        FlyoutHelper.CloseAncestorFlyout(sender);
    }

    private void ButtonOpenCreateOverlayClassPlanFlyout_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedClassPlan == null)
        {
            return;
        }
        ViewModel.OverlayEnableDateTime = ViewModel.ExactTimeService.GetCurrentLocalDateTime().Date;
        ViewModel.TempOverlayClassPlanTimeLayoutId = ViewModel.SelectedClassPlan.TimeLayoutId;
    }
    
    private void ButtonDuplicateClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        var s = ConfigureFileHelper.CopyObject(ViewModel.SelectedClassPlan);
        if (s == null)
        {
            return;
        }

        ViewModel.ProfileService.Profile.ClassPlans.Add(Guid.NewGuid(), s);
        ViewModel.SelectedClassPlan = s;
        UpdateClassPlanInfoEditorTimeLayoutComboBox();
        OpenDrawer("ClassPlansInfoEditor");
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.classPlan.duplicate");
    }
    
    private void ButtonGoToTimeLayoutsPage_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.MasterPageTabSelectIndex = 1;
    }
    
    private void InputElementSubjectItem_OnTapped(object? sender, PointerReleasedEventArgs pointerReleasedEventArgs)
    {
        if (!ViewModel.SettingsService.Settings.IsProfileEditorClassInfoSubjectAutoMoveNextEnabled)
            return;
        if (ViewModel.SelectedClassIndex + 1 >= ViewModel.SelectedClassPlan?.Classes.Count)
        {
            ViewModel.IsClassPlanEditComplete = true;
            if (ViewModel.CurrentClassPlanEditDoneToast != null)
            {
                return;
            }
            var actionButton = new Button()
            {
                Content = "新建课表"
            };
            ViewModel.CurrentClassPlanEditDoneToast = new ToastMessage()
            {
                Severity = InfoBarSeverity.Success,
                Message = "已完成此课表的课程录入。",
                ActionContent = actionButton,
                AutoClose = false
            };
            actionButton.Click += (o, args) =>
            {
                ViewModel.CurrentClassPlanEditDoneToast?.Close();
                CreateClassPlan();
            };
            ViewModel.CurrentClassPlanEditDoneToast.ClosedCancellationTokenSource.Token.Register(() =>
                ViewModel.CurrentClassPlanEditDoneToast = null);
            this.ShowToast(ViewModel.CurrentClassPlanEditDoneToast);
            return;
        }
        ViewModel.SelectedClassIndex++;
        
        DataGridClassPlans.ScrollIntoView(DataGridClassPlans.SelectedItem, DataGridClassPlans.Columns.LastOrDefault());
    }

    #endregion

    #region TimeLayouts
    
    public void UpdateTimeLayout()
    {
        var timeLayout = ViewModel.SelectedTimeLayout;
        if (timeLayout == null)
        {
            return;
        }
        var l = timeLayout.Layouts.ToList();
        l.Sort();
        l.Reverse();
        timeLayout.Layouts = new ObservableCollection<TimeLayoutItem>(l);
        timeLayout.SortCompleted();
    }

    private void ButtonAddTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        var timeLayout = new TimeLayout()
        {
            Name = "新时间表"
        };
        ViewModel.ProfileService.Profile.TimeLayouts.Add(Guid.NewGuid(), timeLayout);
        OpenDrawer("TimeLayoutInfoEditor");
        ViewModel.SelectedTimeLayout = timeLayout;
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.timeLayout.create");
    }
    
    private void ButtonDuplicateTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        var s = ConfigureFileHelper.CopyObject(ViewModel.SelectedTimeLayout);
        if (s == null)
        {
            return;
        }

        OpenDrawer("TimeLayoutInfoEditor");
        ViewModel.ProfileService.Profile.TimeLayouts.Add(Guid.NewGuid(), s);
        ViewModel.SelectedTimeLayout = s;
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.timeLayout.duplicate");
    }
    
    private async void ButtonDeleteTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        var key = ViewModel.ProfileService.Profile.TimeLayouts
            .FirstOrDefault(x => x.Value == ViewModel.SelectedTimeLayout).Key;
        var c = ViewModel.ProfileService.Profile.ClassPlans.Any(x => x.Value.TimeLayoutId == key);
        const string eventName = "views.ProfileSettingsWindow.timeLayout.remove";
        if (c)
        {
            this.ShowWarningToast("仍有课表在使用该时间表。删除时间表前需要删除所有使用该时间表的课表。");
            SentrySdk.Metrics.Increment(eventName, tags: new Dictionary<string, string>
            {
                {"IsSuccess", "false"},
                {"Reason", "仍有课表在使用该时间表。"}
            });
            return;
        }

        SentrySdk.Metrics.Increment(eventName, tags: new Dictionary<string, string>
        {
            {"IsSuccess", "true"}
        });
        ViewModel.ProfileService.Profile.TimeLayouts.Remove(key);
        FlyoutHelper.CloseAncestorFlyout(sender);
    }
    
    private void AddTimePoint(TimeLayoutItem item)
    {
        var timeLayout = ViewModel.SelectedTimeLayout;
        if (timeLayout == null)
        {
            return;
        }
        var l = timeLayout.Layouts;
        for (var i = 0; i < l.Count - 1; i++)
        {
            if (l[i].StartTime <= item.StartTime)
                continue;
            timeLayout.InsertTimePoint(i, item);
            return;
        }
        timeLayout.InsertTimePoint(l.Count, item);
    }
    
    private void AddTimeLayoutItem(int timeType)
    {
        var timeLayout = ViewModel.SelectedTimeLayout;
        var selected   = ViewModel.SelectedTimePoint;
        var baseSec    = (timeType is 0 or 1 ? selected?.EndTime : selected?.StartTime) ?? 
                         // 根据有关规定，中学最早上课时间不得早于 8:00，故将默认的最早时间设定为这个值。
                         // 虽然这么说，但至少我上过和见过的学校里，很少有能履行这一规定的。
                         new TimeSpan(8, 00, 0);
        var settings   = ViewModel.SettingsService.Settings;
        var lastTime   = TimeSpan.FromMinutes(timeType switch
        {
            0 => settings.DefaultOnClassTimePointMinutes,  // 上课
            1 => settings.DefaultBreakingTimePointMinutes, // 课间休息
            2 => 0,  // 分割线
            3 => 0,  // 行动
            _ => 0
        });
        if (timeLayout == null)
        {
            return;
        }
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
                    if (next.StartTime <= baseSec)
                    {
                        if (index != 0)
                        {
                            this.ShowWarningToast("没有合适的位置来插入新的时间点。");
                            return;
                        }
                        baseSec = selected.StartTime - lastTime; // 向前插入时间点的简易实现，未考虑分割线
                        this.ShowToast("已向前插入了新的时间点。");
                    }
                    if (next.StartTime < baseSec + lastTime)
                    {
                        this.ShowToast("没有足够的空间完全插入该时间点，已缩短时间点长度。");
                        lastTime = next.StartTime - baseSec;
                    }
                }
            }

            if (timeType == 2)
            {
                baseSec = selected.EndTime;
                if ((from i in timeLayout.Layouts where i.TimeType == 2 select i.StartTime).ToList().Contains(baseSec))
                {
                    this.ShowWarningToast("这里已经存在一条分割线。");
                    return;
                }
            }

            if (timeType == 3)
            {
                baseSec = selected.EndTime;
                if ((from i in timeLayout.Layouts where i.TimeType == 3 select i.StartTime).ToList().Contains(baseSec))
                {
                    this.ShowWarningToast("这里已经存在一个行动。");
                    return;
                }
            }
        }
        var newItem = new TimeLayoutItem()
        {
            TimeType = timeType,
            StartTime = baseSec,
            EndTime = baseSec + lastTime,
            ActionSet = timeType == 3 ? new ActionSet() : null
        };
        AddTimePoint(newItem);
        // ReSortTimeLayout(newItem);
        ViewModel.SelectedTimePoint = newItem;
        //OpenDrawer("TimePointEditor");
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.timePoint.create", tags: new Dictionary<string, string>()
        {
            {"Type", timeType.ToString()},
            {"Auto", "False"}
        });
    }

    
    private void ButtonAddClassTime_OnClick(object sender, RoutedEventArgs e)
    {
        AddTimeLayoutItem(0);
    }
    
    private void ButtonAddBreakTime_OnClick(object sender, RoutedEventArgs e)
    {
        AddTimeLayoutItem(1);
    }
    
    private void ButtonAddSeparator_OnClick(object sender, RoutedEventArgs e)
    {
        AddTimeLayoutItem(2);
    }
    
    private void ButtonAddActionTimePoint_OnClick(object sender, RoutedEventArgs e)
    {
        AddTimeLayoutItem(3);
    }
    
    private void ButtonRemoveTimePoint_OnClick(object sender, RoutedEventArgs e)
    {
        RemoveSelectedTimePoint();
    }

    private void RemoveSelectedTimePoint()
    {
        if (ViewModel.SelectedTimePoint == null) 
            return;
        var timePoint = ViewModel.SelectedTimePoint;
        var timeLayout = ViewModel.SelectedTimeLayout;
        if (timeLayout == null)
        {
            return;
        }
        var i = timeLayout.Layouts.IndexOf(timePoint);
        timeLayout.RemoveTimePoint(timePoint);
        if (i > 0)
            ViewModel.SelectedTimePoint = timeLayout.Layouts[i - 1];
        var revertButton = new Button()
        {
            Content = "撤销"
        };

        ViewModel.CurrentTimePointDeleteRevertToast?.Close();
        var message = ViewModel.CurrentTimePointDeleteRevertToast = new ToastMessage()
        {
            Message = $"已删除时间点 {timePoint}。",
            Duration = TimeSpan.FromSeconds(10),
            ActionContent = revertButton
        };
        revertButton.Click += RevertButtonOnClick;
        message.ClosedCancellationTokenSource.Token.Register(() =>
        {
            revertButton.Click -= RevertButtonOnClick;
            ViewModel.CurrentTimePointDeleteRevertToast = null;
        });
        ViewModel.ObservableForProperty(x => x.SelectedTimeLayout).Subscribe(_ => message.Close());
        this.ShowToast(message);
        
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.timePoint.remove");
        return;

        void RevertButtonOnClick(object? o, RoutedEventArgs routedEventArgs)
        {
            AddTimePoint(timePoint);
            message.Close();
        }
    }

    private void ButtonRefreshTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        UpdateTimeLayout();
    }
    
    private void ButtonEditTimeLayoutInfo_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("TimeLayoutInfoEditor");
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.timeLayout.edit");
    }
    
    private void ButtonDebugTriggerAction_OnClick(object sender, RoutedEventArgs e)
    {
        var action = ViewModel.SelectedTimePoint?.ActionSet;
        if (action == null)
        {
            return;
        }
        ViewModel.ActionService.InvokeActionSetAsync(action);
    }
    
    private void ButtonOverwriteClasses_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTimePoint == null)
            return;
        var key = ViewModel.ProfileService.Profile.TimeLayouts
            .FirstOrDefault(x => x.Value == ViewModel.SelectedTimeLayout).Key;
        ViewModel.ProfileService.Profile.OverwriteAllClassPlanSubject(
            key,
            ViewModel.SelectedTimePoint,
            ViewModel.SelectedTimePoint.DefaultClassId);
    }
    
    private void CommandBindingRemoveTimePoint_OnExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        RemoveSelectedTimePoint();
    }

    #endregion

    #region Subjects

    private void ButtonAddSubject_OnClick(object sender, RoutedEventArgs e)
    {
        //DataGridSubjects.CancelEdit();
        
        var isCreating = DataGridSubjects.SelectedIndex == ViewModel.ProfileService.Profile.Subjects.Count;
        
        DataGridSubjects.CancelEdit();
        DataGridSubjects.IsReadOnly = true;
        ViewModel.ProfileService.Profile.EditingSubjects.Add(new Subject());
        DataGridSubjects.IsReadOnly = false;
        DataGridSubjects.SelectedIndex = ViewModel.ProfileService.Profile.Subjects.Count - 1;
        //TextBoxSubjectName.Focus();
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.subject.create");
    }
    
    private void ButtonDuplicateSubject_OnClick(object sender, RoutedEventArgs e)
    {
        DataGridSubjects.CancelEdit();
        DataGridSubjects.IsReadOnly = true;
        foreach (var i in DataGridSubjects.SelectedItems)
        {
            var subject = i as Subject;
            var o = ConfigureFileHelper.CopyObject(subject);
            if (o == null)
            {
                continue;
            }

            ViewModel.ProfileService.Profile.EditingSubjects.Add(o);
        }
        DataGridSubjects.SelectedItem = ViewModel.ProfileService.Profile.EditingSubjects.Last();
        DataGridSubjects.IsReadOnly = false;
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.subject.duplicate");
    }

    private void ButtonDeleteSubject_OnClick(object sender, RoutedEventArgs e)
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
        var s = ViewModel.ProfileService.Profile.EditingSubjects;
        foreach (var t in rm)
        {
            s.Remove(t);
        }

        var revertButton = new Button()
        {
            Content = "撤销"
        };
        var toastMessage = new ToastMessage($"已删除 {rm.Count} 个科目。")
        {
            ActionContent = revertButton,
            Duration = TimeSpan.FromSeconds(10)
        };
        revertButton.Click += (o, args) =>
        {
            foreach (var subject in rm)
            {
                ViewModel.ProfileService.Profile.EditingSubjects.Add(subject);
            }
            toastMessage.Close();
        };
        this.ShowToast(toastMessage);
        DataGridSubjects.IsReadOnly = false;
    }
    #endregion

    #region ClassPlanAdjustment

    private (DataGridCell?, int) GetDataGridSelectedCell(DataGrid dataGrid)
    {
        var currentRow = dataGrid.FindDescendantOfType<DataGridRowsPresenter>()?
            .Children.OfType<DataGridRow>()
            .FirstOrDefault(r => r.FindDescendantOfType<DataGridCellsPresenter>()?
                .Children.Any(p => p.Classes.Contains(":current")) ?? false);
        var item = currentRow?.DataContext;

        var children = currentRow?.FindDescendantOfType<DataGridCellsPresenter>()?.Children;
        var currentCell = children?.OfType<DataGridCell>().FirstOrDefault(p => p.Classes.Contains(":current"));
        return (currentCell, currentCell != null ? children?.IndexOf(currentCell) ?? 0 : 0);
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
            var classPlan = ViewModel.LessonsService.GetClassPlanByDate(baseDate.AddDays(i));
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
            (int)Math.Ceiling((baseDate.AddDays(6) - ViewModel.SettingsService.Settings.SingleWeekStartTime).TotalDays / 7);

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
        // if (e.Row.Item is WeekClassPlanRow row &&
        //     GetClassInfoFromRow(row, e.Column.DisplayIndex) == null)
        // {
        //     e.Cancel = true;
        // }
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
        var (cell, colIndex) = GetDataGridSelectedCell(DataGridWeekSchedule);
        if (cell?.DataContext is not WeekClassPlanRow row)
        {
            this.ShowWarningToast("请先选择要交换的课程。");
            return;
        }
        var date = ViewModel.ScheduleWeekViewBaseDate.AddDays(colIndex);
        var index = ViewModel.WeekClassPlanRows.IndexOf(row);
        if (GetClassInfoFromRow(row, colIndex) == null)
        {
            this.ShowWarningToast("选择课程区域无效。");
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
        var (cell, colIndex) = GetDataGridSelectedCell(DataGridWeekSchedule);
        if (cell?.DataContext is not WeekClassPlanRow row)
        {
            return;
        }
        var date = ViewModel.ScheduleWeekViewBaseDate.AddDays(colIndex);
        var index = ViewModel.WeekClassPlanRows.IndexOf(row);
        if (GetClassInfoFromRow(row, colIndex) == null)
        {
            this.ShowWarningToast("选择课程区域无效。");
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

    private ClassPlan? GetTargetClassPlan(DateTime dateTime, bool overlay, out Guid? targetGuid)
    {
        targetGuid = null;
        var baseClassPlan = ViewModel.LessonsService.GetClassPlanByDate(dateTime, out var baseGuid);
        if (baseClassPlan == null || baseGuid == null)
        {
            return null;
        }

        if (!overlay || baseClassPlan.IsOverlay)
        {
            targetGuid = baseGuid.Value;
            return baseClassPlan;
        }

        var orderedClassPlanId = ViewModel.ProfileService.Profile.OrderedSchedules.GetValueOrDefault(dateTime)?.ClassPlanId;
        if (orderedClassPlanId != null
            && ViewModel.ProfileService.Profile.ClassPlans.TryGetValue(orderedClassPlanId.Value, out var classPlan)
            && classPlan.IsOverlay)
        {
            targetGuid = baseGuid.Value;
            return baseClassPlan;
        }

        targetGuid =
            ViewModel.ProfileService.CreateTempClassPlan(baseGuid.Value, enableDateTime: dateTime);
        return targetGuid == null ? null : ViewModel.ProfileService.Profile.ClassPlans.GetValueOrDefault(targetGuid.Value);
    }

    private void ButtonCancelClassSwap_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsInScheduleSwappingMode = false;
    }

    private void ButtonEditClassInfoTemp_OnClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        var (cell, colIndex) = GetDataGridSelectedCell(DataGridWeekSchedule);
        if (cell?.DataContext is not WeekClassPlanRow row)
        {
            this.ShowWarningToast("请先选择要修改的课程。");
            return;
        }
        if (GetClassInfoFromRow(row, colIndex) == null)
        {
            this.ShowWarningToast("选择课程区域无效。");
            return;
        }
        if (sender is CommandBarButton button1 && this.FindResource("ChangeClassFlyout") is Flyout flyout)
        {
            flyout.ShowAt(button1);
        }
        ViewModel.TargetSubjectIndex = Guid.Empty;
        ViewModel.IsClassPlanTempEditPopupOpen = true;
    }

    private void ButtonEditClassInfoTempConfirm_OnClick(object sender, RoutedEventArgs e)
    {
        FlyoutHelper.CloseAncestorFlyout(sender);
        var (cell, colIndex) = GetDataGridSelectedCell(DataGridWeekSchedule);
        if (cell?.DataContext is not WeekClassPlanRow row)
        {
            return;
        }
        var date = ViewModel.ScheduleWeekViewBaseDate.AddDays(colIndex);
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

    private void ScheduleAdjustmentTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ScheduleAdjustmentTabControl?.SelectedIndex != 0)
        {
            return;
        }
        RefreshWeekScheduleRows();
    }

    #endregion
}