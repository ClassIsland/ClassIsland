using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Controls;
using ClassIsland.Controls.ScheduleDataGrid;
using ClassIsland.Controls.TimeLine;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Commands;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.Profile;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Profile;
using ClassIsland.Core.Models.UI;
using ClassIsland.Models;
using ClassIsland.Models.Profile;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Automation;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using FluentAvalonia.UI.Controls;
using HotAvalonia;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Sentry;

namespace ClassIsland.Views;

public partial class ProfileSettingsWindow : MyWindow
{
    private bool _isOpen = false;
    private record UndoEntry(bool IsAdd, TimeLayoutItem Item, TimeLayout Layout, int Index, string Description);
    private readonly Stack<UndoEntry> _undoStack = new();
    private readonly Stack<UndoEntry> _redoStack = new();

    // Drawer 中使用的编辑器实例，替代原来的 x:Name="ReminderEditor"
    private ReminderEditorControl? _drawerReminderEditor;
    private ContentControl? _drawerReminderContent;

    public static readonly FuncValueConverter<ProfileTransferProviderType, string>
        ProfileTransferProviderTypeToImportButtonTextConverter = new(x => x switch
        {
            ProfileTransferProviderType.Import => "导入",
            ProfileTransferProviderType.Export => "导出",
            _ => "执行"
        });

    public ProfileSettingsViewModel ViewModel { get; } = IAppHost.GetService<ProfileSettingsViewModel>();

    private ILogger<ProfileSettingsWindow> Logger => ViewModel.Logger;
    public static ICommand RemoveSelectedTimeLayoutItemCommand { get; } = new RoutedCommand(nameof(RemoveSelectedTimeLayoutItemCommand));

    public ProfileSettingsWindow()
    {
        DataContext = this;
        if (ViewModel.ManagementService.Policy.DisableProfileEditing)
        {
            ViewModel.MasterPageTabSelectIndex = 3;
        }
        InitializeComponent();
        TimeLineListControl.SelectionChanged += TimeLineListControl_OnSelectionChanged;
        TimeLineListControl.KeyDown += OnKeyDown;
        ListViewTimePoints.KeyDown += OnKeyDown;
        // 撤销/重做使用窗口级快捷键，避免依赖时间点列表是否获得焦点
        AddHandler(KeyDownEvent, OnGlobalUndoRedoKeyDown, RoutingStrategies.Tunnel);
        ViewModel.ObservableForProperty(x => x.IsDrawerOpen)
            .Subscribe(_ => OnDrawerStateChanged());
        ViewModel.ObservableForProperty(x => x.SelectedTimeLayout)
            .Subscribe(_ => TimeLineListControl?.ScrollIntoViewCentered(ViewModel.SelectedTimeLayout?.Layouts.FirstOrDefault()));
        ViewModel.ObservableForProperty(x => x.SelectedTimeLayout)
            .Subscribe(_ =>
            {
                _undoStack.Clear(); _redoStack.Clear();
                ViewModel.CanUndo = false; ViewModel.CanRedo = false;
                ViewModel.UndoDescriptions.Clear(); ViewModel.RedoDescriptions.Clear();
            });
        // 日程内联编辑（Drawer 模式）：选中变化时只保存旧值，不自动加载编辑器
        ViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.SelectedReminder))
            {
                SaveReminderEdits();
            }
        };
        // 构建 Drawer 编辑器内容
        BuildReminderDrawerContent();

        // 日程可视化时间线事件
        ReminderTimeline.AddReminderRequested += ReminderTimeline_OnAddReminderRequested;
        ReminderTimeline.ReminderSelected += ReminderTimeline_OnReminderSelected;
        ReminderTimeline.ReminderSettingsClicked += ReminderTimeline_OnReminderSettingsClicked;
    }

    private void OnGlobalUndoRedoKeyDown(object? sender, KeyEventArgs e)
    {
        // 焦点位于文本输入框时，保留其自身的撤销/重做行为
        if (FocusManager?.GetFocusedElement() is TextBox)
            return;
        switch (e.Key)
        {
            case Key.Z when e.KeyModifiers == KeyModifiers.Control:
                UndoLastAction();
                e.Handled = true;
                break;
            case Key.Y when e.KeyModifiers == KeyModifiers.Control:
                RedoLastAction();
                e.Handled = true;
                break;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var layouts = ViewModel.SelectedTimeLayout?.Layouts;
        if (layouts == null || layouts.Count == 0)
            return;

        int currentIndex = ViewModel.SelectedTimePoint != null
            ? layouts.IndexOf(ViewModel.SelectedTimePoint)
            : -1;

        switch (e.Key)
        {
            case Key.Up:
                if (layouts.Count > 0)
                {
                    // 第一个按上 → 最后一个
                    int nextIndex = (currentIndex <= 0)
                        ? layouts.Count - 1
                        : currentIndex - 1;

                    ViewModel.SelectedTimePoint = layouts[nextIndex];
                    TimeLineListControl?.ScrollIntoViewCentered(ViewModel.SelectedTimePoint);
                    e.Handled = true;
                }
                break;
            case Key.Down:
                if (layouts.Count > 0)
                {
                    // 最后一个往下 → 第一个
                    int nextIndex = (currentIndex >= layouts.Count - 1 || currentIndex < 0)
                        ? 0
                        : currentIndex + 1;

                    ViewModel.SelectedTimePoint = layouts[nextIndex];
                    TimeLineListControl?.ScrollIntoViewCentered(ViewModel.SelectedTimePoint);
                    e.Handled = true;
                }
                break;
        }
    }

    private void TimeLineListControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // if (TimeLineListControl?.SelectedItem != null)
        //     TimeLineListControl.ScrollIntoViewCentered(TimeLineListControl.SelectedItem);
    }
    
    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        BuildTransferNavigationItems();
    }

    #region Misc

    private void OnDrawerStateChanged()
    {
        if (!ViewModel.IsDrawerOpen)
        {
            ViewModel.TutorialService.PushToNextSentenceByTag("classisland.profileSettingsWindow.drawer.close");
        }
    }

    public void OpenDrawer(string key)
    {
        ViewModel.IsDrawerOpen = true;
        if (this.FindResource(key) is { } o)
        {
            ViewModel.DrawerContent = o;
        }
    }

    public async void Open(Uri? uri = null)
    {
        if (!_isOpen)
        {
            if (!await ViewModel.ManagementService.AuthorizeByLevel(ViewModel.ManagementService.CredentialConfig
                    .EditProfileAuthorizeLevel))
            {
                return;
            }

            SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.open", 1);
            _isOpen = true;
            Show();
            if (ViewModel.ManagementService.Policy is
                {
                    DisableProfileEditing: false, DisableProfileClassPlanEditing: false,
                    DisableProfileSubjectsEditing: false, DisableProfileTimeLayoutEditing: false
                })
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (ViewModel.ProfileService.Profile.TimeLayouts.Count > 0)
                    {
                        if (ViewModel.ProfileService.Profile.ClassPlans.Count <= 0)
                        {
                            ViewModel.TutorialService.BeginNotCompletedTutorials("classisland.getStarted.profileEditing/setup-classplans");
                        }
                        if (!ViewModel.ProfileService.Profile.ClassPlans.Any(x => x.Value.TimeRule.WeekCountDiv > 0)
                            && ViewModel.ProfileService.Profile.ClassPlans.Count > 0)
                        {
                            ViewModel.TutorialService.BeginNotCompletedTutorials("classisland.getStarted.profileEditing/multiweek-classplans");
                        }
                    }
                    else
                    {
                        ViewModel.TutorialService.BeginNotCompletedTutorials(
                            "classisland.getStarted.profileEditing/concepts",
                            "classisland.getStarted.profileEditing/setup-timeLayout");
                    }
                });
            }
        }
        else
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
        }
        
        ViewModel.TutorialService.PushToNextSentenceByTag("classisland.profileSettingsWindow.open");

        if (uri == null || uri.Segments.Length < 3)
        {
            return;
        }

        var page = uri.Segments[2];
        ViewModel.MasterPageTabSelectIndex = page.ToLower() switch 
        {
            "classplans" when !ViewModel.ManagementService.Policy.DisableProfileEditing => 0,
            "timelayouts" when !ViewModel.ManagementService.Policy.DisableProfileEditing => 1,
            "subjects" when !ViewModel.ManagementService.Policy.DisableProfileEditing => 2,
            "forbidden" => 3,
            "adjustment" => 4,
            "transfer" when ViewModel.ManagementService.Policy is
            {
                DisableProfileEditing: false,
                DisableProfileClassPlanEditing: false,
                DisableProfileTimeLayoutEditing: false,
                DisableProfileSubjectsEditing: false
            } => 5,
            _ => ViewModel.MasterPageTabSelectIndex
        };
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            return;
        }
        e.Cancel = true;
        _isOpen = false;
        SaveReminderEdits(true);
        ViewModel.ProfileService.SaveProfile();
        Hide();
    }
    
    private void MasterTabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (ViewModel.MasterPageTabSelectIndex == 0 && ViewModel.ProfileService.Profile.TimeLayouts.Count > 0
                                                        && ViewModel.ProfileService.Profile.ClassPlans.Count <= 0)
            {
                ViewModel.TutorialService.BeginNotCompletedTutorials("classisland.getStarted.profileEditing/setup-classplans");
            }
        });
    }
    
    private void ButtonHelp_OnClick(object? sender, RoutedEventArgs e)
    {
        UriNavigationCommands.UriNavigationCommand.Execute("https://docs.classisland.tech/app/profile/");
    }

    #endregion

    #region Reminders

    private Reminder? _lastEditedReminder;

    // 防抖：合并短时间内的多次日程编辑写入请求
    private CancellationTokenSource? _reminderSaveDebounceCts = null;
    private static readonly TimeSpan ReminderSaveDebounceInterval = TimeSpan.FromMilliseconds(500);

    private void SaveReminderEdits(bool immediate = false)
    {
        if (_lastEditedReminder != null && _drawerReminderEditor != null)
        {
            _drawerReminderEditor.ApplyTo(_lastEditedReminder);

            if (immediate)
            {
                // 窗口/抽屉关闭时立即保存，确保数据不丢失
                _lastEditedReminder.NotifyPropertiesChanged();
                ViewModel.ProfileService.SaveProfile();
                var reminderService = IAppHost.GetService<ScheduleReminderService>();
                reminderService?.RequestImmediateCheck();
            }
            else
            {
                // 编辑中防抖：
                //   ApplyTo 即时更新内存（Title 通过 setter 自然触发 PropertyChanged，实时刷新标题）；
                //   全量 PropertyChanged 通知 + 磁盘写入合并到防抖到期后执行，避免每按一次键都触发
                //   11 个 PropertyChanged 事件导致 UI 卡顿。
                var reminderToNotify = _lastEditedReminder;
                var oldCts = _reminderSaveDebounceCts;
                _reminderSaveDebounceCts = new CancellationTokenSource();
                oldCts?.Cancel();
                oldCts?.Dispose();
                var token = _reminderSaveDebounceCts.Token;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(ReminderSaveDebounceInterval, token).ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();

                        // 全量 UI 通知（等待 UI 线程执行完成，避免 Post 的 fire-and-forget 问题）
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            reminderToNotify.NotifyPropertiesChanged();
                        }).GetTask().ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        ViewModel.ProfileService.SaveProfile();
                        var reminderService = IAppHost.GetService<ScheduleReminderService>();
                        reminderService?.RequestImmediateCheck();
                    }
                    catch (OperationCanceledException)
                    {
                        // 被新的编辑请求取消，属于正常流程
                    }
                }, token);
            }
        }
    }

    private void LoadReminderIntoEditor()
    {
        _lastEditedReminder = ViewModel.SelectedReminder;
        if (ViewModel.SelectedReminder != null && _drawerReminderEditor != null)
        {
            _drawerReminderEditor.LoadFrom(ViewModel.SelectedReminder);
        }
    }

    private void ButtonOpenReminders_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenScheduleTab();
    }

    private void BuildReminderDrawerContent()
    {
        if (_drawerReminderContent != null) return;

        var editor = new ReminderEditorControl();
        var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel { Spacing = 12, Margin = new Thickness(16) };

        // 标题栏
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var title = new TextBlock
        {
            Text = "编辑日程",
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        var closeBtn = new Button
        {
            Content = new TextBlock
            {
                Text = "✕",
                FontSize = 16,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            },
            Width = 32,
            Height = 32,
            Padding = new Thickness(0)
        };
        closeBtn.Click += (_, _) => CloseReminderDrawer();
        Grid.SetColumn(title, 0);
        Grid.SetColumn(closeBtn, 1);
        headerGrid.Children.Add(title);
        headerGrid.Children.Add(closeBtn);

        stack.Children.Add(headerGrid);
        stack.Children.Add(editor);
        scrollViewer.Content = stack;

        _drawerReminderContent = new ContentControl
        {
            Content = scrollViewer,
            Width = 420
        };
        _drawerReminderEditor = editor;

        // 编辑器实时变更 → 立即同步
        _drawerReminderEditor.EditingChanged += (_, _) => SaveReminderEdits();
    }

    private void OpenReminderDrawer(Reminder reminder)
    {
        if (_drawerReminderContent == null) return;
        _lastEditedReminder = reminder;
        _drawerReminderEditor?.LoadFrom(reminder);
        ViewModel.DrawerContent = _drawerReminderContent;
        ViewModel.IsDrawerOpen = true;
    }

    private void CloseReminderDrawer()
    {
        SaveReminderEdits(true);
        ViewModel.IsDrawerOpen = false;
    }

    private void ButtonOpenReminderDrawer_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: Reminder reminder })
        {
            ViewModel.SelectedReminder = reminder;
            OpenReminderDrawer(reminder);
        }
    }

    private void ButtonShowReminderDetails_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedReminder != null)
        {
            OpenReminderDrawer(ViewModel.SelectedReminder);
        }
    }

    public void OpenScheduleTab()
    {
        if (!IsVisible)
        {
            Show();
        }

        MasterTabControl.SelectedItem = ScheduleTabItem;
    }

    private void AddReminder_OnClick(object? sender, RoutedEventArgs e)
    {
        var reminder = new Reminder() { Time = DateTime.Now };
        ViewModel.AddReminder(reminder);
        ViewModel.SelectedReminder = reminder;
    }

    private void RemoveSelectedReminder_OnClick(object? sender, RoutedEventArgs e)
    {
        var sel = ViewModel.SelectedReminder;
        if (sel == null) return;
        ViewModel.RemoveReminder(sel);
        ViewModel.SelectedReminder = null;
    }

    #endregion

    #region 日程可视化时间线

    private void ButtonReminderTimelineZoomIn_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ReminderTimelineScale = Math.Min(10.0, ViewModel.ReminderTimelineScale + 0.5);
    }

    private void ButtonReminderTimelineZoomOut_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ReminderTimelineScale = Math.Max(0.5, ViewModel.ReminderTimelineScale - 0.5);
    }

    private void ReminderTimeline_OnAddReminderRequested(object? sender, DateTime targetTime)
    {
        var reminder = new Reminder()
        {
            Time = targetTime,
            TimeOfDay = targetTime.TimeOfDay,
            Title = "新日程"
        };
        ViewModel.AddReminder(reminder);
        ViewModel.SelectedReminder = reminder;
    }

    private void ReminderTimeline_OnReminderSelected(object? sender, Reminder? reminder)
    {
        ViewModel.SelectedReminder = reminder;
    }

    private void ReminderTimeline_OnReminderSettingsClicked(object? sender, Reminder? reminder)
    {
        if (reminder != null)
        {
            ViewModel.SelectedReminder = reminder;
            OpenReminderDrawer(reminder);
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

    private async void ButtonTrustProfile_OnClick(object? sender, RoutedEventArgs e)
    {
        var result = await new ContentDialog()
        {
            Title = "不信任的档案",
            Content = "当前档案不受信任，部分功能（如行动时间点等）将禁用。如果您信任此档案并希望启用这些受限的功能，请将此档案设置为信任。",
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "信任此档案",
            SecondaryButtonText = "取消"
        }.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            ViewModel.ProfileService.TrustCurrentProfile();
        }
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
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.profile.create", 1);
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
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.profile.openFolder", 1);
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(Services.ProfileService.ProfilePath),
            UseShellExecute = true
        });
    }

    private void ButtonRefreshProfiles_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.profile.refresh", 1);
        RefreshProfiles();
    }

    private async void MenuItemRenameProfile_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.profile.rename", 1);
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
            SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.profile.remove", 1,
                [
                    new KeyValuePair<string, object>("Reason", "正在删除已加载或将要加载的档案。"),
                    new KeyValuePair<string, object>("IsSuccess", "false" ) 
                ]
                );
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
            SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.profile.remove", 1,
                [
                    new KeyValuePair<string, object>("IsSuccess", "true")
                ]);
            File.Delete(path);
        }
        else
        {
            SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.profile.remove", 1,
                [
                    new KeyValuePair<string, object>("Reason", "用户取消操作。"),
                    new KeyValuePair<string, object>("IsSuccess", "false")
                ]
                );
        }

        RefreshProfiles();
    }

    private void MenuItemProfileDuplicate_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.profile.duplicate", 1);
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
    
    private void ButtonOpenProfileImportPage_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.MasterPageTabSelectIndex = 5;
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
    
    private void TreeViewClassPlans_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ViewModel.CurrentClassPlanEditDoneToast?.Close();
    }
    
    private void ButtonCreateTempOverlayClassPlan_OnClick(object? sender, RoutedEventArgs e)
    {
        var key = ViewModel.ProfileService.Profile.ClassPlans
            .FirstOrDefault(x => x.Value == ViewModel.SelectedClassPlan).Key;
        var id = ViewModel.ProfileService.CreateTempClassPlan(key,
            ViewModel.TempOverlayClassPlanTimeLayoutId,
            ViewModel.OverlayEnableDateTime,
            ViewModel.TempOverlayCreateTimeLayout);
        if (id is { } guid)
        {
            ViewModel.SelectClassPlanByGuid(guid);
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
        var selectedNode = ViewModel.SelectedClassPlansTreeNode;
        if (selectedNode is not null)
        {
            var selectedClassPlanGroup = 
                selectedNode.IsGroup
                    ? selectedNode.Guid
                    : selectedNode.ClassPlan?.AssociatedGroup;

            newClassPlan.AssociatedGroup = selectedClassPlanGroup ?? ViewModel.ProfileService.Profile.SelectedClassPlanGroupId;
        }
        else
        {
            newClassPlan.AssociatedGroup = ViewModel.ProfileService.Profile.SelectedClassPlanGroupId;
        }
        
        var newClassPlanGuid = Guid.NewGuid();
        ViewModel.ProfileService.Profile.ClassPlans.Add(newClassPlanGuid, newClassPlan);
        ViewModel.SelectClassPlanByGuid(newClassPlanGuid);
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
        ViewModel.SelectedClassPlansTreeNode = null;
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

        var newClassPlanGuid = Guid.NewGuid();
        ViewModel.ProfileService.Profile.ClassPlans.Add(newClassPlanGuid, s);
        ViewModel.SelectClassPlanByGuid(newClassPlanGuid);
        UpdateClassPlanInfoEditorTimeLayoutComboBox();
        OpenDrawer("ClassPlansInfoEditor");
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.classPlan.duplicate", 1);
    }
    
    private void ButtonGoToTimeLayoutsPage_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.MasterPageTabSelectIndex = 1;
    }
    
    private void InputElementSubjectItem_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel.SelectedClassInfo != null && sender is Border { DataContext: KeyValuePair<Guid, Subject> kvp })
        {
            ViewModel.SelectedClassInfo.SubjectId = kvp.Key;
        }
        
    }
    
    private void InputElementSubjectItem_OnTapped(object? sender, PointerReleasedEventArgs pointerReleasedEventArgs)
    {
        if (!ViewModel.SettingsService.Settings.IsProfileEditorClassInfoSubjectAutoMoveNextEnabled)
            return;
        if (ViewModel.SelectedClassIndex + 1 >= ViewModel.SelectedClassPlan?.Classes.Count)
        {
            ViewModel.IsClassPlanEditComplete = true;
            var targetDate = ViewModel.ScheduleCalendarSelectedDate.AddDays(1);
            if (ViewModel.SettingsService.Settings.ClassPlanEditModeIndex == 1)
            {
                if (targetDate.DayOfWeek < ViewModel.ScheduleCalendarSelectedDate.DayOfWeek)
                {
                    this.ShowSuccessToast("已完成本周课表的录入。");
                    return;
                }
                if (ViewModel.LessonsService.GetClassPlanByDate(targetDate) != null)
                {
                    ViewModel.ScheduleCalendarSelectedDate = targetDate;
                    ViewModel.SelectedClassIndex = 0;
                    ScheduleDataGrid.ScrollIntoCurrentView();
                    this.ShowToast("已跳转到次日课表。");
                    return;
                }
            }
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
                if (ViewModel.SettingsService.Settings.ClassPlanEditModeIndex == 0)
                {
                    CreateClassPlan();
                }
                else
                {
                    ScheduleDataGrid.CreateClassPlanByDate(targetDate);
                    ScheduleDataGrid.ScrollIntoCurrentView();
                }
            };
            ViewModel.CurrentClassPlanEditDoneToast.ClosedCancellationTokenSource.Token.Register(() =>
                ViewModel.CurrentClassPlanEditDoneToast = null);
            this.ShowToast(ViewModel.CurrentClassPlanEditDoneToast);
            return;
        }
        ViewModel.SelectedClassIndex++;

        if (DataGridClassPlans.IsLoaded && ViewModel.SettingsService.Settings.ClassPlanEditModeIndex == 0)
        {
            DataGridClassPlans.ScrollIntoView(DataGridClassPlans.SelectedItem, DataGridClassPlans.Columns.LastOrDefault());
        } else if (ScheduleDataGrid.IsLoaded && ViewModel.SettingsService.Settings.ClassPlanEditModeIndex == 1)
        {
            ScheduleDataGrid.ScrollIntoCurrentView();
        }
    }
    
    private void ButtonRefreshScheduleDataGrid_OnClick(object? sender, RoutedEventArgs e)
    {
        ScheduleDataGrid.RefreshWeekScheduleRows();
        ScheduleCalendarControl2.UpdateSchedule();
    }
    
    private void ScheduleDataGrid_OnOpenClassPlanSettingsRequested(object? sender, ScheduleDataGridClassPlanEventArgs e)
    {
        ViewModel.SelectClassPlanByInstance(e.ClassPlan);
        // ViewModel.ScheduleCalendarSelectedDate = e.Date;
        UpdateClassPlanInfoEditorTimeLayoutComboBox();
        OpenDrawer("ClassPlansInfoEditor");
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
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.timeLayout.create", 1);
        ViewModel.TutorialService.PushToNextSentence("classisland.getStarted.profileEditing/setup-timeLayout");
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
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.timeLayout.duplicate", 1);
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
            SentrySdk.Metrics.EmitCounter(eventName, 1,
            [
                new KeyValuePair<string, object>("IsSuccess", "false"),
                new KeyValuePair<string, object>("Reason", "仍有课表在使用该时间表。")
            ]
            );
            return;
        }

        SentrySdk.Metrics.EmitCounter(eventName, 1,
        [
            new KeyValuePair<string, object>("IsSuccess", "true")
        ]
        );
        ViewModel.ProfileService.Profile.TimeLayouts.Remove(key);
        FlyoutHelper.CloseAncestorFlyout(sender);
    }
    
    private void PushAddUndo(TimeLayoutItem item, TimeLayout layout)
    {
        var index = layout.Layouts.IndexOf(item);
        var desc = $"添加{item}";
        _undoStack.Push(new UndoEntry(IsAdd: true, item, layout, index, desc));
        ViewModel.UndoDescriptions.Insert(0, desc);
        _redoStack.Clear();
        ViewModel.RedoDescriptions.Clear();
        ViewModel.CanUndo = true;
        ViewModel.CanRedo = false;
    }

    private void PushDeleteUndo(TimeLayoutItem item, TimeLayout layout, int index)
    {
        var desc = $"删除{item}";
        _undoStack.Push(new UndoEntry(IsAdd: false, item, layout, index, desc));
        ViewModel.UndoDescriptions.Insert(0, desc);
        _redoStack.Clear();
        ViewModel.RedoDescriptions.Clear();
        ViewModel.CanUndo = true;
        ViewModel.CanRedo = false;
    }

    private void UndoLastAction()
    {
        if (!_undoStack.TryPop(out var entry)) return;
        var undoneIndex = entry.IsAdd ? entry.Layout.Layouts.IndexOf(entry.Item) : entry.Index;
        if (entry.IsAdd)
        {
            entry.Layout.RemoveTimePoint(entry.Item);
            if (ViewModel.SelectedTimePoint == entry.Item)
                ViewModel.SelectedTimePoint = entry.Layout.Layouts.Count > 0 ? entry.Layout.Layouts[Math.Max(0, undoneIndex - 1)] : null;
        }
        else
        {
            entry.Layout.InsertTimePoint(entry.Index, entry.Item);
            ViewModel.SelectedTimePoint = entry.Item;
        }
        if (ViewModel.UndoDescriptions.Count > 0) ViewModel.UndoDescriptions.RemoveAt(0);
        _redoStack.Push(entry with { Index = undoneIndex });
        ViewModel.RedoDescriptions.Insert(0, entry.Description);
        ViewModel.CanUndo = _undoStack.Count > 0;
        ViewModel.CanRedo = true;
    }

    private void RedoLastAction()
    {
        if (!_redoStack.TryPop(out var entry)) return;
        int redoneIndex;
        if (entry.IsAdd)
        {
            entry.Layout.InsertTimePoint(entry.Index, entry.Item);
            ViewModel.SelectedTimePoint = entry.Item;
            redoneIndex = entry.Index;
        }
        else
        {
            redoneIndex = entry.Layout.Layouts.IndexOf(entry.Item);
            entry.Layout.RemoveTimePoint(entry.Item);
            if (ViewModel.SelectedTimePoint == entry.Item)
                ViewModel.SelectedTimePoint = entry.Layout.Layouts.Count > 0 ? entry.Layout.Layouts[Math.Max(0, redoneIndex - 1)] : null;
        }
        if (ViewModel.RedoDescriptions.Count > 0) ViewModel.RedoDescriptions.RemoveAt(0);
        _undoStack.Push(entry with { Index = redoneIndex });
        ViewModel.UndoDescriptions.Insert(0, entry.Description);
        ViewModel.CanRedo = _redoStack.Count > 0;
        ViewModel.CanUndo = true;
    }

    private void UndoToIndex(int count)
    {
        for (var i = 0; i < count; i++) UndoLastAction();
    }

    private void RedoToIndex(int count)
    {
        for (var i = 0; i < count; i++) RedoLastAction();
    }

    private void ButtonUndoAdd_OnClick(object? sender, RoutedEventArgs e)
    {
        FlyoutHelper.CloseAncestorFlyout(sender as Control);
        UndoLastAction();
    }

    private void ButtonRedoAdd_OnClick(object? sender, RoutedEventArgs e)
    {
        FlyoutHelper.CloseAncestorFlyout(sender as Control);
        RedoLastAction();
    }

    private void UndoHistoryList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lb || lb.SelectedIndex < 0) return;
        var count = lb.SelectedIndex + 1;
        lb.SelectedIndex = -1;
        FlyoutHelper.CloseAncestorFlyout(lb);
        UndoToIndex(count);
    }

    private void RedoHistoryList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lb || lb.SelectedIndex < 0) return;
        var count = lb.SelectedIndex + 1;
        lb.SelectedIndex = -1;
        FlyoutHelper.CloseAncestorFlyout(lb);
        RedoToIndex(count);
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
        TimeLineListControl?.ScrollIntoViewCentered(item);
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
        PushAddUndo(newItem, timeLayout);
        //OpenDrawer("TimePointEditor");
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.timePoint.create", 1,
        [
            new KeyValuePair<string, object>("Type", timeType.ToString()),
            new KeyValuePair<string, object>("Auto", "False")
        ]
        );
        ViewModel.TutorialService.PushToNextSentence();
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
    
    private void TimeLineListControl_OnRequestInsertTimePoint(object? sender, TimeLineInsertTimePointEventArgs e)
    {
        if (e.Location != TimeLineInsertTimePointEventArgs.InsertLocation.After)
        {
            return;  // 未实现
        }

        ViewModel.SelectedTimePoint = e.TimePoint;
        AddTimeLayoutItem(e.Kind);
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
        if (timeLayout == null) return;
        var i = timeLayout.Layouts.IndexOf(timePoint);
        timeLayout.RemoveTimePoint(timePoint);
        if (i > 0)
            ViewModel.SelectedTimePoint = timeLayout.Layouts[i - 1];
        PushDeleteUndo(timePoint, timeLayout, i);
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.timePoint.remove", 1);
    }

    private void ButtonRefreshTimeLayout_OnClick(object sender, RoutedEventArgs e)
    {
        UpdateTimeLayout();
    }
    
    private void ButtonEditTimeLayoutInfo_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("TimeLayoutInfoEditor");
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.timeLayout.edit", 1);
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

    private void StackPanelBreakingTimePointSettings_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        ViewModel.CurrentProfileBreakNames = new HashSet<string>(
            ViewModel.ProfileService.Profile.TimeLayouts.Values
                .SelectMany(t => t.Layouts
                .Select(l => l.BreakName))
                .Where(n => !string.IsNullOrEmpty(n)));
        // Console.WriteLine(string.Join(" ", ViewModel.CurrentProfileBreakNames));
    }
    
    private void ButtonZoomOut_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TimeLineScale > 1.0)
        {
            ViewModel.TimeLineScale -= 0.2;
        }
        ViewModel.TimeLineScale = Math.Round(ViewModel.TimeLineScale, 1);
        if (TimeLineListControl.SelectedItem != null)
            TimeLineListControl.ScrollIntoViewCentered(TimeLineListControl.SelectedItem);
    }

    private void ButtonZoomIn_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TimeLineScale < 5.0)
        {
            ViewModel.TimeLineScale += 0.2;
        }
        ViewModel.TimeLineScale = Math.Round(ViewModel.TimeLineScale, 1);
        if (TimeLineListControl.SelectedItem != null)
            TimeLineListControl.ScrollIntoViewCentered(TimeLineListControl.SelectedItem);
    }

    private void ButtonTimeLayoutItemScrollIntoItem(object? sender, RoutedEventArgs e)
    {
        var timeLayoutItems = ViewModel.SelectedTimeLayout?.Layouts;
        var tpr = ViewModel.SelectedTimePoint ?? (timeLayoutItems is { Count: > 0 } ? timeLayoutItems?[0] : null);
        if (tpr == null)
        {
            return;
        }

        ViewModel.SelectedTimePoint = null;
        ViewModel.SelectedTimePoint = tpr;
        TimeLineListControl.ScrollIntoViewCentered(tpr);
        ListViewTimePoints.ScrollIntoView(tpr);
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
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.subject.create", 1);
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
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.subject.duplicate", 1);
    }

    private void ButtonDeleteSubject_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.EmitCounter("views.ProfileSettingsWindow.subject.remove", 1,
        [
            new KeyValuePair<string, object>("IsSuccess", "true")
        ]
        );

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
    
    private void ButtonRefreshScheduleAdjustmentView_OnClick(object sender, RoutedEventArgs e)
    {
        ScheduleDataGridAdjustment.RefreshWeekScheduleRows();
        ScheduleCalendarControl.UpdateSchedule();
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
        var date = ViewModel.ScheduleCalendarSelectedDate;
        var index = ViewModel.SelectedClassIndex2;
        if (ViewModel.SelectedClassInfo == null || ViewModel.SelectedClassInfo.IsEmpty)
        {
            this.ShowWarningToast("选择课程区域无效。");
            return;
        }
        ViewModel.ClassSwapStartPosition = new ScheduleClassPosition(date, index);
        ViewModel.IsInScheduleSwappingMode = true;
    }

    private void ButtonSwapScheduleComplete_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsInScheduleSwappingMode = false;
        var date = ViewModel.ScheduleCalendarSelectedDate;
        var index = ViewModel.SelectedClassIndex2;
        if (ViewModel.SelectedClassInfo == null || ViewModel.SelectedClassInfo.IsEmpty)
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
        var date = ViewModel.ScheduleCalendarSelectedDate;
        var index = ViewModel.SelectedClassIndex2;
        if (ViewModel.SelectedClassInfo == null || ViewModel.SelectedClassInfo.IsEmpty)
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
        var date = ViewModel.ScheduleCalendarSelectedDate;
        var index = ViewModel.SelectedClassIndex2;

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
        
        ScheduleCalendarControl.UpdateSchedule();
    }

    private void ScheduleAdjustmentTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ScheduleAdjustmentTabControl?.SelectedIndex != 0)
        {
            return;
        }
    }

    #endregion

    #region ProfileTransfer

    [AvaloniaHotReload]
    private void BuildTransferNavigationItems()
    {
        TransferNavigationView.MenuItems.Clear();
        var infos = IProfileTransferService.Providers
            .OrderBy(x => x.Type)
            .GroupBy(x => x.Type)
            .ToList();
        foreach (var info in infos)
        {
            if (info != infos.FirstOrDefault())
            {
                TransferNavigationView.MenuItems.Add(new NavigationViewItemSeparator());
            }
            if (info.Key != ProfileTransferProviderType.None)
            {
                TransferNavigationView.MenuItems.Add(new NavigationViewItemHeader()
                {
                    Content = info.Key switch
                    {
                        ProfileTransferProviderType.Import => "导入",
                        ProfileTransferProviderType.Export => "导出",
                        _ => "？？？"
                    }
                });    
            }
            
            TransferNavigationView.MenuItems.AddRange(info.Select(x => new NavigationViewItem()
            {
                IconSource = x.Icon,
                Content = x.Name,
                Tag = x
            }));
            
        }
    }
    
    private void TransferNavigationView_OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is not NavigationViewItem { Tag: ProfileTransferProviderInfo info })
        {
            return;
        }

        if (info.FunctionHandler != null)
        {
            info.FunctionHandler(this);
            return;
        }

        if (info.HandlerControlType == null)
        {
            return;
        }

        if (Activator.CreateInstance(info.HandlerControlType) is not ProfileTransferProviderControlBase control)
        {
            return;
        }

        ViewModel.TransferProviderContent = control;
        ViewModel.SelectedTransferInfo = info;
        ViewModel.IsProfileTransferInvoked = false;
    }

    private async void ButtonInvokeTransfer_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.TransferProviderContent == null || ViewModel.SelectedTransferInfo == null)
        {
            return;
        }

        var operationText = ViewModel.SelectedTransferInfo.Type switch
        {
            ProfileTransferProviderType.Import => "导入",
            ProfileTransferProviderType.Export => "导出",
            _ => "迁移"
        };

        if (ViewModel.SelectedTransferInfo.Type == ProfileTransferProviderType.Import && ViewModel.IsProfileTransferInvoked)
        {
            var t = await ContentDialogHelper.ShowConfirmationDialog($"要继续{operationText}吗",
                $"您先前已经成功地{operationText}了档案，您还要继续{operationText}吗？", positiveText: "继续");
            if (!t)
            {
                return;
            }
        }

        ViewModel.IsTransferring = true;
        try
        {
            var result = await ViewModel.TransferProviderContent.InvokeTransfer();
            if (!result)
            {
                return;
            }

            ViewModel.IsProfileTransferInvoked = true;
        }
        finally
        {
            ViewModel.IsTransferring = false;
        }
    }
    
    #endregion
}