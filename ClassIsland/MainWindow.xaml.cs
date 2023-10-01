using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ClassIsland.Enums;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.ViewModels;
using ClassIsland.Views;
using H.NotifyIcon;
using MaterialDesignThemes.Wpf;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.Win32;
using Path = System.IO.Path;
using Window = System.Windows.Window;

namespace ClassIsland;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainViewModel ViewModel
    {
        get;
        set;
    }

    public ProfileSettingsWindow ProfileSettingsWindow
    {
        get;
        set;
    }

    public SettingsWindow SettingsWindow
    {
        get;
        set;
    }

    public DispatcherTimer UpdateTimer
    {
        get;
    } = new(DispatcherPriority.Render)
    {
        Interval = TimeSpan.FromMilliseconds(25)
    };

    private HelpsWindow HelpsWindow
    {
        get;
    }

    private SettingsService SettingsService
    {
        get;
    }

    private TaskBarIconService TaskBarIconService
    {
        get;
    }

    private ThemeService ThemeService
    {
        get;
    }

    public NotificationHostService NotificationHostService
    {
        get;
    }

    public ProfileService ProfileService
    {
        get;
    }

    private Stopwatch UserPrefrenceUpdateStopwatch
    {
        get;
    } = new();

    public MiniInfoProviderHostService MiniInfoProviderHostService
    {
        get;
    } = App.GetService<MiniInfoProviderHostService>();

    public MainWindow(SettingsService settingsService, ProfileService profileService, NotificationHostService notificationHostService, TaskBarIconService taskBarIconService, ThemeService themeService)
    {
        SettingsService = settingsService;
        TaskBarIconService = taskBarIconService;
        NotificationHostService = notificationHostService;
        ThemeService = themeService;
        ProfileService = profileService;

        SettingsService.PropertyChanged += (sender, args) =>
        {
            LoadSettings();
        };
        TaskBarIconService.MainTaskBarIcon.TrayBalloonTipClicked += TaskBarIconOnTrayBalloonTipClicked;
        UpdateTimer.Tick += UpdateTimerOnTick;
        DataContext = this;
        UpdateTimer.Start();
        ViewModel = new MainViewModel();
        ProfileSettingsWindow = new ProfileSettingsWindow
        {
            MainViewModel = ViewModel
        };
        ProfileSettingsWindow.Closing += (o, args) => SaveProfile();
        SettingsWindow = new SettingsWindow()
        {
            MainViewModel = ViewModel,
            Settings = ViewModel.Settings
        };
        SettingsWindow.Closed += (o, args) => SaveSettings();
        HelpsWindow = new HelpsWindow();
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        InitializeComponent();
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.CurrentStatus))
        {
            NotificationHostService.CurrentState = ViewModel.CurrentStatus;
            NotificationHostService.OnCurrentStateChanged(this, EventArgs.Empty);
        }
    }

    private void TaskBarIconOnTrayBalloonTipClicked(object sender, RoutedEventArgs e)
    {
        OpenSettingsWindow();
        SettingsWindow.RootTabControl.SelectedIndex = 5;
    }

    private int GetSubjectIndex(int index)
    {
        var k = ViewModel.CurrentClassPlan?.TimeLayout.Layouts[index];
        var l = (from t in ViewModel.CurrentClassPlan?.TimeLayout.Layouts where t.TimeType == 0 select t).ToList();
        var i = l.IndexOf(k);
        return i;
    }

    private Storyboard BeginStoryboard(string name)
    {
        var a = (Storyboard)FindResource(name);
        a.Begin();
        return a;
    }

    private void UpdateMouseStatus()
    {
        NativeWindowHelper.GetCursorPos(out var ptr);
        GetCurrentDpi(out var dpiX, out var dpiY);
        //Debug.WriteLine($"Window: {Left * dpiX} {Top * dpiY};; Cursor: {ptr.X} {ptr.Y} ;; dpi: {dpiX}");
        var cx = Left * dpiX;
        var cy = Top * dpiY;
        var cw = Width * dpiX;
        var ch = Height * dpiY;
        var cr = cx + cw;
        var cb = cy + ch;

        ViewModel.IsMouseIn = (cx <= ptr.X && cy <= ptr.Y && ptr.X <= cr && ptr.Y <= cb);
        
    }

    private async void UpdateTimerOnTick(object? sender, EventArgs e)
    {
        UpdateWindowPos();
        UpdateMouseStatus();
        LoadCurrentClassPlan();
        if (ViewModel.Settings.WindowLayer == 0)
        {
            //SetBottom();
        }
        NotificationHostService.OnUpdateTimerTick(this, EventArgs.Empty);

        // Detect fullscreen
        var screen = Screen.AllScreens[ViewModel.Settings.WindowDockingMonitorIndex] ??
                            Screen.PrimaryScreen;
        ViewModel.IsForegroundFullscreen = NativeWindowHelper.IsForegroundFullScreen(screen);
        ViewModel.IsForegroundMaxWindow = NativeWindowHelper.IsForegroundMaxWindow(screen);

        // Deactivate
        foreach (var i in ViewModel.Profile.TimeLayouts)
        {
            i.Value.IsActivated = false;
        }
        foreach (var i in ViewModel.Profile.ClassPlans)
        {
            i.Value.IsActivated = false;
        }

        if (ViewModel.CurrentClassPlan is null || ViewModel.CurrentClassPlan.TimeLayout is null)
        {
            ViewModel.CurrentStatus = TimeState.None;
            ViewModel.CurrentOverlayStatus = TimeState.None;
            ViewModel.CurrentOverlayEventStatus = TimeState.None;
            NotificationHostService.IsClassPlanLoaded = false;
            goto final;
        }
        NotificationHostService.IsClassPlanLoaded = true;
        // Activate selected item
        ViewModel.CurrentClassPlan.IsActivated = true;
        ViewModel.CurrentClassPlan.TimeLayout.IsActivated = true;

        var isLessonConfirmed = false;
        // 更新选择
        var currentLayout = ViewModel.CurrentClassPlan.TimeLayout.Layouts;
        foreach (var i in currentLayout)
        {
            if (i.StartSecond.TimeOfDay <= DateTime.Now.TimeOfDay && i.EndSecond.TimeOfDay >= DateTime.Now.TimeOfDay)
            {
                ViewModel.CurrentSelectedIndex = currentLayout.IndexOf(i);
                ViewModel.CurrentTimeLayoutItem = i;
                NotificationHostService.IsClassConfirmed = isLessonConfirmed = true;
                if (ViewModel.CurrentTimeLayoutItem.TimeType == 0)
                {
                    var i0 = GetSubjectIndex(currentLayout.IndexOf(i));
                    ViewModel.CurrentSubject = ViewModel.Profile.Subjects[ViewModel.CurrentClassPlan.Classes[i0].SubjectId];
                }
                else
                {
                    ViewModel.CurrentSubject = null;
                }
                break;
            }
        }

        //var isBreaking = false;
        if (!isLessonConfirmed)
        {
            ViewModel.CurrentSelectedIndex = null;
            ViewModel.CurrentStatus = TimeState.None;
        }
        // 获取下节课信息
        else if (ViewModel.CurrentSelectedIndex + 1 < currentLayout.Count && ViewModel.CurrentSelectedIndex is not null)
        {
            var nextClassTimeLayoutItems = (from i in currentLayout
                where currentLayout.IndexOf(i) > ViewModel.CurrentSelectedIndex
                      && i.TimeType == 0
                select i)
                .ToList();
            var nextBreakingTimeLayoutItems = (from i in currentLayout
                    where currentLayout.IndexOf(i) > ViewModel.CurrentSelectedIndex
                          && i.TimeType == 1
                    select i)
                .ToList();
            if (nextClassTimeLayoutItems.Count > 0)
            {
                var i0 = GetSubjectIndex(currentLayout.IndexOf(nextClassTimeLayoutItems[0]));
                var index = ViewModel.CurrentClassPlan.Classes[i0].SubjectId;
                NotificationHostService.NextClassSubject = ViewModel.NextSubject = ViewModel.Profile.Subjects[index];
                NotificationHostService.NextClassTimeLayoutItem = ViewModel.NextTimeLayoutItem = nextClassTimeLayoutItems[0];
            }

            if (nextBreakingTimeLayoutItems.Count > 0)
            {
                NotificationHostService.NextBreakingTimeLayoutItem = ViewModel.NextBreakingLayoutItem = nextBreakingTimeLayoutItems[0];
            }
        }

        var tClassDelta = ViewModel.NextTimeLayoutItem.StartSecond.TimeOfDay - DateTime.Now.TimeOfDay;
        ViewModel.OnClassLeftTime = tClassDelta;
        NotificationHostService.OnClassDeltaTime = tClassDelta;
        NotificationHostService.OnBreakingTimeDeltaTime =
            ViewModel.NextBreakingLayoutItem.StartSecond.TimeOfDay - DateTime.Now.TimeOfDay;
        // 获取状态信息
        if (ViewModel.CurrentSelectedIndex == null)
        {
            ViewModel.CurrentStatus = TimeState.None;
        //} else if (tClassDelta > TimeSpan.Zero && tClassDelta <= TimeSpan.FromSeconds(ViewModel.Settings.ClassPrepareNotifySeconds))
        //{
        //    ViewModel.CurrentStatus = TimeState.PrepareOnClass;
        } else if (ViewModel.CurrentTimeLayoutItem.TimeType == 0)
        {
            ViewModel.CurrentStatus = TimeState.OnClass;
        } else if (ViewModel.CurrentTimeLayoutItem.TimeType == 1)
        {
            ViewModel.CurrentStatus = TimeState.Breaking;
        }

        switch (ViewModel.CurrentStatus)
        {
            // 向提醒提供方传递事件
            // 下课事件
            case TimeState.Breaking when ViewModel.CurrentOverlayEventStatus != TimeState.Breaking:
                NotificationHostService.OnOnBreakingTime(this, EventArgs.Empty);
                ViewModel.CurrentOverlayEventStatus = TimeState.Breaking;
                break;
            // 上课事件
            case TimeState.OnClass when ViewModel.CurrentOverlayEventStatus != TimeState.OnClass:
                NotificationHostService.OnOnClass(this, EventArgs.Empty);
                ViewModel.CurrentOverlayEventStatus = TimeState.OnClass;
                break;
            case TimeState.None:
                break;
            case TimeState.PrepareOnClass:
                break;
            default:
                break;
        }

        final:
        // 处理提醒请求队列
        if (!ViewModel.IsOverlayOpened)
        {
            ViewModel.IsOverlayOpened = true;
            if (!ViewModel.Settings.IsNotificationEnabled)
            {
                NotificationHostService.RequestQueue.Clear();
            }
            while (NotificationHostService.RequestQueue.Count > 0)
            {
                var request = ViewModel.CurrentNotificationRequest = NotificationHostService.GetRequest();
                if (request.TargetMaskEndTime != null)
                {
                    request.MaskDuration = request.TargetMaskEndTime.Value - DateTime.Now;
                }
                if (request.TargetOverlayEndTime != null)
                {
                    request.OverlayDuration = request.TargetOverlayEndTime.Value - DateTime.Now - request.MaskDuration;
                }
                ViewModel.CurrentMaskElement = request.MaskContent;
                var cancellationToken = request.CancellationTokenSource.Token;

                if (request.MaskDuration > TimeSpan.Zero &&
                    request.OverlayDuration > TimeSpan.Zero)
                {
                    BeginStoryboard("OverlayMaskIn");
                    await Task.Run(() => cancellationToken.WaitHandle.WaitOne(request.MaskDuration), cancellationToken);
                    if (request.OverlayContent is null || cancellationToken.IsCancellationRequested)
                    {
                        BeginStoryboard("OverlayMaskOutDirect");
                    }
                    else
                    {
                        ViewModel.CurrentOverlayElement = request.OverlayContent;
                        BeginStoryboard("OverlayMaskOut");
                        ViewModel.OverlayRemainStopwatch.Restart();
                        await Task.Run(() => cancellationToken.WaitHandle.WaitOne(request.OverlayDuration), cancellationToken);
                        ViewModel.OverlayRemainStopwatch.Stop();
                    }

                }
                if (NotificationHostService.RequestQueue.Count < 1)
                {
                    BeginStoryboard("OverlayOut");
                }
            }

            ViewModel.CurrentOverlayElement = null;
            ViewModel.CurrentMaskElement = null;
            ViewModel.IsOverlayOpened = false;
        }

        // Update percents
        if (ViewModel.IsOverlayOpened && 
            ViewModel.OverlayRemainStopwatch.IsRunning)
        {
            var request = ViewModel.CurrentNotificationRequest;
            var totalMs = request.OverlayDuration.TotalMilliseconds;
            var ms = ViewModel.OverlayRemainStopwatch.ElapsedMilliseconds;
            if (totalMs > 0)
            {
                ViewModel.OverlayRemainTimePercents = (totalMs - ms) / totalMs;
            }
        }

        // Finished update
        ViewModel.Today = DateTime.Now;
        MainListBox.SelectedIndex = ViewModel.CurrentSelectedIndex ?? -1;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        UpdateTheme();
        var menu = (ContextMenu)FindResource("AppContextMenu");
        menu.DataContext = this;
        TaskBarIconService.MainTaskBarIcon.ContextMenu = menu;
        TaskBarIconService.MainTaskBarIcon.DataContext = this;
        ViewModel.OverlayRemainTimePercents = 0.5;

        if (!ViewModel.Settings.IsWelcomeWindowShowed)
        {
            var w = new WelcomeWindow()
            {
                ViewModel =
                {
                    Settings = ViewModel.Settings
                }
            };
            var r = w.ShowDialog();
            if (r == false)
            {
                ViewModel.IsClosing = true;
                Close();
            }
            else
            {
                ViewModel.Settings.IsWelcomeWindowShowed = true;
            }
        }
        base.OnContentRendered(e);
    }

    public void LoadProfile()
    {
        ProfileService.LoadProfile();
        ViewModel.Profile = ProfileService.Profile;
    }

    public void SaveProfile()
    {
        ProfileService.SaveProfile();
    }

    private void LoadSettings()
    {
        var r = SettingsService.Settings;
        ViewModel.Settings = r;
        ViewModel.Settings.PropertyChanged += (sender, args) => SaveSettings();
        SettingsWindow.Settings = r;
    }

    public void SaveSettings()
    {
        UpdateTheme();
        SettingsService.SaveSettings();
    }

    protected override void OnInitialized(EventArgs e)
    {
        if (!Directory.Exists("./Profiles"))
        {
            Directory.CreateDirectory("./Profiles");
        }
        base.OnInitialized(e);
        ViewModel.Profile.PropertyChanged += (sender, args) => SaveProfile();
        ViewModel.Settings.PropertyChanged += (sender, args) => SaveSettings();
        LoadSettings();
        ViewModel.CurrentProfilePath = ViewModel.Settings.SelectedProfile;
        LoadProfile();
        UpdateTheme();
        UserPrefrenceUpdateStopwatch.Start();
        SystemEvents.UserPreferenceChanged += OnSystemEventsOnUserPreferenceChanged;
    }

    private void OnSystemEventsOnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs args)
    {
        if (UserPrefrenceUpdateStopwatch.ElapsedMilliseconds < 1000)
        {
            return;
        }
        //Debug.WriteLine("Updated theme.");
        UserPrefrenceUpdateStopwatch.Restart();
        UpdateTheme();
    }

    private void SetBottom()
    {
        if (ViewModel.Settings.WindowLayer != 0)
        {
            return;
        }
        var hWnd = new WindowInteropHelper(this).Handle;
        NativeWindowHelper.SetWindowPos(hWnd, NativeWindowHelper.HWND_BOTTOM, 0, 0, 0, 0, NativeWindowHelper.SWP_NOSIZE | NativeWindowHelper.SWP_NOMOVE | NativeWindowHelper.SWP_NOACTIVATE);
    }

    private async void UpdateTheme()
    {
        var aState = await AppCenter.IsEnabledAsync();
        if (aState != ViewModel.Settings.IsReportingEnabled)
        {
            await AppCenter.SetEnabledAsync(ViewModel.Settings.IsReportingEnabled);
        }

        UpdateWindowPos();
        var hWnd = new WindowInteropHelper(this).Handle;
        var style = NativeWindowHelper.GetWindowLong(hWnd, NativeWindowHelper.GWL_EXSTYLE);
        style |= NativeWindowHelper.WS_EX_TOOLWINDOW;
        if (!ViewModel.Settings.IsMouseClickingEnabled)
        {
            var r = NativeWindowHelper.SetWindowLong(hWnd, NativeWindowHelper.GWL_EXSTYLE, style | NativeWindowHelper.WS_EX_TRANSPARENT);
        }
        else
        {
            style &= ~(uint)NativeWindowHelper.WS_EX_TRANSPARENT;
            var r = NativeWindowHelper.SetWindowLong(hWnd, NativeWindowHelper.GWL_EXSTYLE, style);
        }

        switch (ViewModel.Settings.WindowLayer)
        {
            case 0: // bottom
                Topmost = false;
                break;
            case 1:
                Topmost = true;
                break;
        }

        var primary = Colors.DodgerBlue;
        var secondary = Colors.DodgerBlue;
        switch (ViewModel.Settings.ColorSource)
        {
            case 0: //custom
                primary = ViewModel.Settings.PrimaryColor;
                secondary = ViewModel.Settings.SecondaryColor;
                break;
            case 1:
                primary = secondary = ViewModel.Settings.SelectedPlatte;
                break;
            case 2:
                try
                {
                    NativeWindowHelper.DwmGetColorizationColor(out var color, out _);
                    var c = NativeWindowHelper.GetColor(color);
                    primary = secondary = c;
                }
                catch
                {
                    // ignored
                }
                break;

        }
        ThemeService.SetTheme(ViewModel.Settings.Theme, primary, secondary);
    }

    private void ButtonSettings_OnClick(object sender, RoutedEventArgs e)
    {
        OpenProfileSettingsWindow();
    }

    public bool CheckClassPlan(ClassPlan plan)
    {
        if (plan.TimeRule.WeekDay != (int)DateTime.Now.DayOfWeek)
        {
            return false;
        }

        var dd = DateTime.Now.Date - ViewModel.Settings.SingleWeekStartTime.Date;
        var dw = Math.Floor(dd.TotalDays / 7) + 1;
        var w = (int)dw % 2;
        switch (plan.TimeRule.WeekCountDiv)
        {
            case 1 when w != 1:
                return false;
            case 2 when w != 0:
                return false;
            default:
                return true;
        }
    }

    public void LoadCurrentClassPlan()
    {
        ViewModel.Profile.RefreshTimeLayouts();
        if (ViewModel.TemporaryClassPlanSetupTime.Date < DateTime.Now.Date)  // 清除过期临时课表
        {
            ViewModel.TemporaryClassPlan = null;
        }

        if (ViewModel.Profile.IsOverlayClassPlanEnabled && 
            ViewModel.Profile.OverlayClassPlanId != null &&
            ViewModel.Profile.ClassPlans.ContainsKey(ViewModel.Profile.OverlayClassPlanId))
        {
            ViewModel.CurrentClassPlan = ViewModel.Profile.ClassPlans[ViewModel.Profile.OverlayClassPlanId];
            return;
        }
        var a = (from p in ViewModel.Profile.ClassPlans
            where CheckClassPlan(p.Value)
            select p.Value)
            .ToList();
        ViewModel.CurrentClassPlan = ViewModel.TemporaryClassPlan?.Value ?? (a.Count < 1 ? null : a[0]!);
    }

    private void ListView_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        e.Handled = true;
    }

    private void ButtonResizeDebug_OnClick(object sender, RoutedEventArgs e)
    {
        SizeToContent = SizeToContent.WidthAndHeight;
    }

    private void MainWindow_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            e.Handled = true;
            //DragMove();
        }
    }

    private void MenuItemSettings_OnClick(object sender, RoutedEventArgs e)
    {
        OpenSettingsWindow();
    }

    private void OpenSettingsWindow()
    {
        if (!SettingsWindow.IsOpened)
        {
            Analytics.TrackEvent("打开设置窗口");
            SettingsWindow.IsOpened = true;
            SettingsWindow.Show();
        }
        else
        {
            if (SettingsWindow.WindowState == WindowState.Minimized)
            {
                SettingsWindow.WindowState = WindowState.Normal;
            }

            SettingsWindow.Activate();
        }
    }

    private void MenuItemDebugOverlayMaskIn_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CurrentMaskElement = FindResource("ClassPrepareNotifyMask");
        ViewModel.CurrentOverlayElement = FindResource("ClassPrepareNotifyOverlay");
        var a = (Storyboard)FindResource("OverlayMaskIn");
        a.Begin();
    }

    private void MenuItemDebugOverlayMaskOut_OnClick(object sender, RoutedEventArgs e)
    {
        var a = (Storyboard)FindResource("OverlayMaskOut");
        a.Begin();
    }

    private void MenuItemDebugOverlayOut_OnClick(object sender, RoutedEventArgs e)
    {
        var a = (Storyboard)FindResource("OverlayOut");
        a.Begin();
    }

    private void MenuItemDebugOverlayMaskOutDirect_OnClick(object sender, RoutedEventArgs e)
    {
        BeginStoryboard("OverlayMaskOutDirect");
    }

    private void MenuItemExitApp_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsClosing = true;
        Close();
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (!ViewModel.IsClosing)
        {
            e.Cancel = true;
            return;
        }
        SaveProfile();
        SaveSettings();
    }

    private void UpdateWindowPos()
    {
        GetCurrentDpi(out var dpiX, out var dpiY);

        var scale = ViewModel.Settings.Scale;
        Width = GridRoot.ActualWidth * scale;
        Height = GridRoot.ActualHeight * scale;
        ViewModel.GridRootLeft = Width / 10 * (scale - 1);
        ViewModel.GridRootTop = Height / 10 * (scale - 1);

        var screen = ViewModel.Settings.WindowDockingMonitorIndex < Screen.AllScreens.Length 
            ? Screen.AllScreens[ViewModel.Settings.WindowDockingMonitorIndex] 
            : Screen.PrimaryScreen;
        var aw = RenderSize.Width * dpiX;
        var ah = RenderSize.Height * dpiY;
        var c = (double)(screen.WorkingArea.Left + screen.WorkingArea.Right) / 2;
        var ox = ViewModel.Settings.WindowDockingOffsetX;
        var oy = ViewModel.Settings.WindowDockingOffsetY;

        switch (ViewModel.Settings.WindowDockingLocation)
        {
            case 0: //左上
                Left = (screen.WorkingArea.Left + ox) / dpiX;
                Top = (screen.WorkingArea.Top + oy) / dpiY;
                break;
            case 1: // 中上
                Left = (c - aw / 2 + ox) / dpiX;
                Top = (screen.WorkingArea.Top + oy) / dpiY;
                break;
            case 2: // 右上
                Left = (screen.WorkingArea.Right - aw + ox) / dpiX;
                Top = (screen.WorkingArea.Top + oy) / dpiY;
                break;
            case 3: // 左下
                Left = (screen.WorkingArea.Left + ox) / dpiX;
                Top = (screen.WorkingArea.Bottom - ah + oy) / dpiY;
                break;
            case 4: // 中下
                Left = (c - aw / 2 + ox) / dpiX;
                Top = (screen.WorkingArea.Bottom - ah + oy) / dpiY;
                break;
            case 5: // 右下
                Left = (screen.WorkingArea.Right - aw + ox) / dpiX;
                Top = (screen.WorkingArea.Bottom - ah + oy) / dpiY;
                break;
        }
    }

    public void GetCurrentDpi(out double dpiX, out double dpiY)
    {
        var source = PresentationSource.FromVisual(this);

        dpiX = 1.0;
        dpiY = 1.0;

        if (source?.CompositionTarget != null)
        {
            dpiX = 1.0 * source.CompositionTarget.TransformToDevice.M11;
            dpiY = 1.0 * source.CompositionTarget.TransformToDevice.M22;
        }
    }

    private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateWindowPos();
    }

    private void MainWindow_OnActivated(object? sender, EventArgs e)
    {
        SetBottom();
    }

    private void MainWindow_OnStateChanged(object? sender, EventArgs e)
    {
        SetBottom();
    }

    private void MenuItemTemporaryClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        ProfileSettingsWindow.OpenDrawer("TemporaryClassPlan");
        OpenProfileSettingsWindow();
    }

    private void OpenProfileSettingsWindow()
    {
        if (!ProfileSettingsWindow.IsOpened)
        {
            Analytics.TrackEvent("打开档案设置窗口");
            ProfileSettingsWindow.IsOpened = true;
            ProfileSettingsWindow.Show();
        }
        else
        {
            if (ProfileSettingsWindow.WindowState == WindowState.Minimized)
            {
                ProfileSettingsWindow.WindowState = WindowState.Normal;
            }
            ProfileSettingsWindow.Activate();
        }
    }

    private void MenuItemAbout_OnClick(object sender, RoutedEventArgs e)
    {
        OpenSettingsWindow();
        SettingsWindow.RootTabControl.SelectedIndex = 7;
    }

    private void MenuItemDebugWelcomeWindow_OnClick(object sender, RoutedEventArgs e)
    {
        var ww = new WelcomeWindow();
        ww.ShowDialog();
    }

    private void MenuItemDebugWelcomeWindow2_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.Settings.IsWelcomeWindowShowed = false;
        SaveSettings();
    }

    private void MenuItemHelps_OnClick(object sender, RoutedEventArgs e)
    {
        OpenHelpsWindow();
    }

    public void OpenHelpsWindow()
    {
        Analytics.TrackEvent("打开帮助窗口");
        if (HelpsWindow.ViewModel.IsOpened)
        {
            HelpsWindow.WindowState = HelpsWindow.WindowState == WindowState.Minimized ? WindowState.Normal : HelpsWindow.WindowState;
            HelpsWindow.Activate();
        }
        else
        {
            HelpsWindow.ViewModel.IsOpened = true;
            HelpsWindow.Show();
        }
    }

    private void MenuItemUpdates_OnClick(object sender, RoutedEventArgs e)
    {
        OpenSettingsWindow();
        SettingsWindow.RootTabControl.SelectedIndex = 5;
    }

    private void GridRoot_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Width = e.NewSize.Width * ViewModel.Settings.Scale;
        Height = e.NewSize.Height * ViewModel.Settings.Scale;
    }

    private void GridContent_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        GridWrapper.Width = e.NewSize.Width + 32;
    }

    private async void MenuItemDebugFitSize_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OverlayRemainTimePercents = 0.5;
    }

    private void MenuItemClearAllNotifications_OnClick(object sender, RoutedEventArgs e)
    {
        NotificationHostService.CurrentRequest?.CancellationTokenSource.Cancel();
    }

    private void MenuItemNotificationSettings_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsWindow.RootTabControl.SelectedIndex = 2;
        OpenSettingsWindow();
    }

    private void MenuItemShowMainWindow_OnChecked(object sender, RoutedEventArgs e)
    {
        TaskBarIconService.MainTaskBarIcon.IconSource = new GeneratedIconSource()
        {
            BackgroundSource =
                new BitmapImage(new Uri("pack://application:,,,/ClassIsland;component/Assets/AppLogo.png",
                    UriKind.Absolute)),
        };
    }

    private void MenuItemShowMainWindow_OnUnchecked(object sender, RoutedEventArgs e)
    {
        TaskBarIconService.MainTaskBarIcon.IconSource = new GeneratedIconSource()
        {
            BackgroundSource =
                new BitmapImage(new Uri("pack://application:,,,/ClassIsland;component/Assets/AppLogo_Fade.png",
                    UriKind.Absolute)),
        };
    }
}
