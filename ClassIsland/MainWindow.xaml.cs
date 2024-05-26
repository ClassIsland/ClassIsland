using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using ClassIsland.Controls.NotificationEffects;
using ClassIsland.Core.Abstraction.Models;
using ClassIsland.Core.Abstraction.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Interfaces;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Models.Profile;
using ClassIsland.Services;
using ClassIsland.ViewModels;
using ClassIsland.Views;

using H.NotifyIcon;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using NAudio.Wave;

using Application = System.Windows.Application;
using Window = System.Windows.Window;

namespace ClassIsland;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static readonly ICommand TrayIconLeftClickedCommand = new RoutedCommand();

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
        Interval = TimeSpan.FromMilliseconds(50)
    };

    private Storyboard NotificationProgressBar { get; set; } = new Storyboard();

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

    public TopmostEffectWindow TopmostEffectWindow { get; }

    private Stopwatch UserPrefrenceUpdateStopwatch
    {
        get;
    } = new();

    private ExactTimeService ExactTimeService { get; }

    public ISpeechService SpeechService { get; }

    private ILogger<MainWindow> Logger;

    private double _latestDpiX = 1.0;
    private double _latestDpiY = 1.0;

    public ClassChangingWindow? ClassChangingWindow { get; set; }

    public MiniInfoProviderHostService MiniInfoProviderHostService
    {
        get;
    } = App.GetService<MiniInfoProviderHostService>();

    public static readonly DependencyProperty BackgroundWidthProperty = DependencyProperty.Register(
        nameof(BackgroundWidth), typeof(double), typeof(MainWindow), new PropertyMetadata(0.0));

    public double BackgroundWidth
    {
        get { return (double)GetValue(BackgroundWidthProperty); }
        set { SetValue(BackgroundWidthProperty, value); }
    }

    public MainWindow(SettingsService settingsService, 
        ProfileService profileService,
        NotificationHostService notificationHostService, 
        TaskBarIconService taskBarIconService,
        ThemeService themeService, 
        ILogger<MainWindow> logger, 
        ISpeechService speechService,
        ExactTimeService exactTimeService,
        TopmostEffectWindow topmostEffectWindow)
    {
        Logger = logger;
        SpeechService = speechService;
        SettingsService = settingsService;
        TaskBarIconService = taskBarIconService;
        NotificationHostService = notificationHostService;
        ThemeService = themeService;
        ProfileService = profileService;
        ExactTimeService = exactTimeService;
        TopmostEffectWindow = topmostEffectWindow;

        SettingsService.PropertyChanged += (sender, args) =>
        {
            LoadSettings();
        };
        TaskBarIconService.MainTaskBarIcon.TrayBalloonTipClicked += TaskBarIconOnTrayBalloonTipClicked;
        UpdateTimer.Tick += UpdateTimerOnTick;
        DataContext = this;
        UpdateTimer.Start();
        ViewModel = new MainViewModel();
        ProfileSettingsWindow = App.GetService<ProfileSettingsWindow>();
        ProfileSettingsWindow.MainViewModel = ViewModel;
        ProfileSettingsWindow.Closing += (o, args) => SaveProfile();
        SettingsWindow = new SettingsWindow()
        {
            MainViewModel = ViewModel,
            Settings = ViewModel.Settings
        };
        SettingsWindow.Closed += (o, args) => SaveSettings();
        HelpsWindow = App.GetService<HelpsWindow>();
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
        if (PresentationSource.FromVisual(this) == null)
        {
            return;
        }
        GetCursorPos(out var ptr);
        GetCurrentDpi(out var dpiX, out var dpiY);
        var scale = ViewModel.Settings.Scale;
        //Debug.WriteLine($"Window: {Left * dpiX} {Top * dpiY};; Cursor: {ptr.X} {ptr.Y} ;; dpi: {dpiX}");
        var root = GridWrapper.PointToScreen(new Point(0, 0));
        var cx = root.X;
        var cy = root.Y;
        var cw = GridWrapper.ActualWidth * dpiX * scale;
        var ch = GridWrapper.ActualHeight * dpiY * scale;
        var cr = cx + cw;
        var cb = cy + ch;

        ViewModel.IsMouseIn = (cx <= ptr.X && cy <= ptr.Y && ptr.X <= cr && ptr.Y <= cb);
        
    }

    private async void UpdateTimerOnTick(object? sender, EventArgs e)
    {
        SettingsService.Settings.IsNetworkConnect = InternetGetConnectedState(out var _);
        SettingsService.Settings.IsNotificationSpeechEnabled = SettingsService.Settings.IsNetworkConnect || SettingsService.Settings.IsSystemSpeechSystemExist;
        if (SettingsService.Settings.IsMainWindowDebugEnabled)
            ViewModel.DebugCurrentTime = ExactTimeService.GetCurrentLocalDateTime();

        UpdateWindowPos(true);
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
        foreach (var i in ViewModel.Profile.TimeLayouts.Where(i => !i.Value.IsActivatedManually))
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
            if (i.StartSecond.TimeOfDay <= ExactTimeService.GetCurrentLocalDateTime().TimeOfDay && i.EndSecond.TimeOfDay >= ExactTimeService.GetCurrentLocalDateTime().TimeOfDay)
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
                NotificationHostService.NextClassSubject = ViewModel.NextSubject = ViewModel.Profile.Subjects[index] ?? Subject.Empty;
                NotificationHostService.NextClassTimeLayoutItem = ViewModel.NextTimeLayoutItem = nextClassTimeLayoutItems[0];
            }

            if (nextBreakingTimeLayoutItems.Count > 0)
            {
                NotificationHostService.NextBreakingTimeLayoutItem = ViewModel.NextBreakingLayoutItem = nextBreakingTimeLayoutItems[0];
            }
        }

        var tClassDelta = ViewModel.NextTimeLayoutItem.StartSecond.TimeOfDay - ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        ViewModel.OnClassLeftTime = tClassDelta;
        NotificationHostService.OnClassDeltaTime = tClassDelta;
        NotificationHostService.OnBreakingTimeDeltaTime =
            ViewModel.NextBreakingLayoutItem.StartSecond.TimeOfDay - ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
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
                Logger.LogInformation("发出下课事件。");
                NotificationHostService.OnOnBreakingTime(this, EventArgs.Empty);
                ViewModel.CurrentOverlayEventStatus = TimeState.Breaking;
                break;
            // 上课事件
            case TimeState.OnClass when ViewModel.CurrentOverlayEventStatus != TimeState.OnClass:
                Logger.LogInformation("发出上课事件。");
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
        await ProcessNotification();

        // Finished update
        ViewModel.Today = ExactTimeService.GetCurrentLocalDateTime();
        MainListBox.SelectedIndex = ViewModel.CurrentSelectedIndex ?? -1;
    }

    public Point GetCenter()
    {
        GetCurrentDpi(out var dpi, out _);
        var p = GridWrapper.TranslatePoint(new Point(GridWrapper.ActualWidth / 2, GridWrapper.ActualHeight / 2), this);
        p.Y = Top + ActualHeight / 2;
        return p;
    }

    private async Task ProcessNotification()
    {
        if (ViewModel.IsOverlayOpened)
        {
            return;
        }
        ViewModel.IsOverlayOpened = true;  // 上锁

        if (ViewModel.FirstProcessNotifications == DateTime.MinValue)
            ViewModel.FirstProcessNotifications = ExactTimeService.GetCurrentLocalDateTime();
        if (!ViewModel.Settings.IsNotificationEnabled ||
            (ExactTimeService.GetCurrentLocalDateTime() - ViewModel.FirstProcessNotifications <= TimeSpan.FromSeconds(10) &&
             App.ApplicationCommand.Quiet) // 静默启动
           )
        {
            NotificationHostService.RequestQueue.Clear();
        }

        while (NotificationHostService.RequestQueue.Count > 0)
        {
            using var player = new DirectSoundOut();
            var request = ViewModel.CurrentNotificationRequest = NotificationHostService.GetRequest();  // 获取当前的通知请求
            var settings = ViewModel.Settings as INotificationSettings;
            foreach (var i in new List<NotificationSettings>([request.ProviderSettings, request.RequestNotificationSettings]).Where(i => i.IsSettingsEnabled))
            {
                settings = i;
                break;
            }
            var isSpeechEnabled = settings.IsSpeechEnabled && request.IsSpeechEnabled && ViewModel.Settings.AllowNotificationSpeech;
            Logger.LogInformation("处理通知请求：{} {}", request.MaskContent.GetType(), request.OverlayContent?.GetType());
            if (request.TargetMaskEndTime != null)  // 如果目标结束时间为空，那么就计算持续时间
            {
                request.MaskDuration = request.TargetMaskEndTime.Value - ExactTimeService.GetCurrentLocalDateTime();
            }

            if (request.TargetOverlayEndTime != null)  // 如果目标结束时间为空，那么就计算持续时间
            {
                request.OverlayDuration = request.TargetOverlayEndTime.Value - ExactTimeService.GetCurrentLocalDateTime() - request.MaskDuration;
            }

            ViewModel.CurrentMaskElement = request.MaskContent;  // 加载Mask元素
            var cancellationToken = request.CancellationTokenSource.Token;
            ViewModel.IsNotificationWindowExplicitShowed = settings.IsNotificationTopmostEnabled && ViewModel.Settings.AllowNotificationTopmost;
            if (ViewModel.IsNotificationWindowExplicitShowed && ViewModel.Settings.WindowLayer == 0)  // 如果处于置底状态，还需要激活窗口来强制显示窗口。
            {
                Activate();
            }

            if (request.MaskDuration > TimeSpan.Zero &&
                request.OverlayDuration > TimeSpan.Zero)
            {
                if (isSpeechEnabled)
                {
                    SpeechService.EnqueueSpeechQueue(request.MaskSpeechContent);
                }
                BeginStoryboard("OverlayMaskIn");
                // 播放提醒音效
                if (settings.IsNotificationSoundEnabled && ViewModel.Settings.AllowNotificationSound)
                {
                    try
                    {
                        IWaveProvider provider = string.IsNullOrWhiteSpace(settings.NotificationSoundPath)
                            ? new StreamMediaFoundationReader(
                                Application.GetResourceStream(INotificationProvider.DefaultNotificationSoundUri)!.Stream)
                            : new AudioFileReader(settings.NotificationSoundPath);
                        player.Init(provider);
                        player.Play();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "无法播放提醒音效：{}", settings.NotificationSoundPath);
                    }
                }
                // 播放提醒特效
                if (settings.IsNotificationEffectEnabled && ViewModel.Settings.AllowNotificationEffect &&
                    GridRoot.IsVisible && ViewModel.IsMainWindowVisible)
                {
                    TopmostEffectWindow.PlayEffect(new RippleEffect()
                    {
                        CenterX = GetCenter().X,
                        CenterY = GetCenter().Y
                    });
                }
                await Task.Run(() => cancellationToken.WaitHandle.WaitOne(request.MaskDuration), cancellationToken);
                if (request.OverlayContent is null || cancellationToken.IsCancellationRequested)
                {
                    BeginStoryboard("OverlayMaskOutDirect");
                }
                else
                {
                    ViewModel.CurrentOverlayElement = request.OverlayContent;
                    if (isSpeechEnabled)
                    {
                        SpeechService.EnqueueSpeechQueue(request.OverlaySpeechContent);
                    }
                    BeginStoryboard("OverlayMaskOut");
                    ViewModel.OverlayRemainStopwatch.Restart();
                    // 倒计时动画
                    var da = new DoubleAnimation()
                    {
                        From = 1.0,
                        To = 0.0,
                        Duration = new Duration(request.OverlayDuration),

                    };
                    var storyboard = new Storyboard()
                    {
                    };
                    Storyboard.SetTarget(da, OverlayTimeProgressBar);
                    Storyboard.SetTargetProperty(da, new PropertyPath(RangeBase.ValueProperty));
                    storyboard.Children.Add(da);
                    storyboard.Begin();
                    await Task.Run(() => cancellationToken.WaitHandle.WaitOne(request.OverlayDuration),
                        cancellationToken);
                    ViewModel.OverlayRemainStopwatch.Stop();
                }
                SpeechService.ClearSpeechQueue();
            }

            if (NotificationHostService.RequestQueue.Count < 1)
            {
                BeginStoryboard("OverlayOut");
            }
            await request.CompletedTokenSource.CancelAsync();
        }

        ViewModel.CurrentOverlayElement = null;
        ViewModel.CurrentMaskElement = null;
        ViewModel.IsOverlayOpened = false;
        ViewModel.IsNotificationWindowExplicitShowed = false;
        SetBottom();
        UpdateTheme();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        if (DesignerProperties.GetIsInDesignMode(this))
            return;
        UpdateTheme();
        var menu = (ContextMenu)FindResource("AppContextMenu");
        menu.DataContext = this;
        TaskBarIconService.MainTaskBarIcon.DataContext = this;
        TaskBarIconService.MainTaskBarIcon.ContextMenu = menu;
        TaskBarIconService.MainTaskBarIcon.LeftClickCommand = TrayIconLeftClickedCommand;
        TaskBarIconService.MainTaskBarIcon.TrayLeftMouseUp += MainTaskBarIconOnTrayLeftMouseUp;
        ViewModel.OverlayRemainTimePercents = 0.5;
        DiagnosticService.EndStartup();
        if (ViewModel.Settings.IsSplashEnabled)
        {
            App.GetService<SplashService>().EndSplash();
        }

        if (!ViewModel.Settings.IsNotificationEffectRenderingScaleAutoSet)
        {
            AutoSetNotificationEffectRenderingScale();
        }

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

    private void AutoSetNotificationEffectRenderingScale()
    {
        var screen = ViewModel.Settings.WindowDockingMonitorIndex < Screen.AllScreens.Length
            ? Screen.AllScreens[ViewModel.Settings.WindowDockingMonitorIndex]
            : Screen.PrimaryScreen;
        if (screen == null)
            return;
        if (screen.Bounds.Height >= 1400)
        {
            ViewModel.Settings.NotificationEffectRenderingScale = 0.75;
        }
        if (screen.Bounds.Height >= 2000)
        {
            ViewModel.Settings.NotificationEffectRenderingScale = 0.5;
        }

        ViewModel.Settings.IsNotificationEffectRenderingScaleAutoSet = true;
    }

    private void MainTaskBarIconOnTrayLeftMouseUp(object sender, RoutedEventArgs e)
    {
        switch (ViewModel.Settings.TaskBarIconClickBehavior)
        {
            case 1:
                OpenProfileSettingsWindow();
                break;
            case 2:
                ViewModel.IsMainWindowVisible = !ViewModel.IsMainWindowVisible;
                break;
        }
    }

    public void LoadProfile()
    {
        //ProfileService.LoadProfile();
        ViewModel.Profile = ProfileService.Profile;
    }

    public void SaveProfile()
    {
        ProfileService.SaveProfile(ViewModel.CurrentProfilePath);
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
        base.OnInitialized(e);
        if (DesignerProperties.GetIsInDesignMode(this))
            return;
        ViewModel.Profile.PropertyChanged += (sender, args) => SaveProfile();
        ViewModel.Settings.PropertyChanged += (sender, args) => SaveSettings();
        LoadSettings();
        //ViewModel.CurrentProfilePath = ViewModel.Settings.SelectedProfile;
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
        var hWnd = (HWND)new WindowInteropHelper(this).Handle;
        if (ViewModel.Settings.WindowLayer != 0)
        {
            return;
        }
        if (ViewModel.IsNotificationWindowExplicitShowed)
        {
            SetWindowPos(hWnd, default, 0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
            return;
        }
        SetWindowPos(hWnd, NativeWindowHelper.HWND_BOTTOM, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    private async void UpdateTheme()
    {
        var aState = await AppCenter.IsEnabledAsync();
        if (aState != ViewModel.Settings.IsReportingEnabled)
        {
            await AppCenter.SetEnabledAsync(ViewModel.Settings.IsReportingEnabled);
        }

        UpdateWindowPos();
        var hWnd = (HWND)new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        style |= NativeWindowHelper.WS_EX_TOOLWINDOW;
        if (!ViewModel.Settings.IsMouseClickingEnabled)
        {
            var r = SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style | NativeWindowHelper.WS_EX_TRANSPARENT);
        }
        else
        {
            style &= ~NativeWindowHelper.WS_EX_TRANSPARENT;
            var r = SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style);
        }

        switch (ViewModel.Settings.WindowLayer)
        {
            case 0: // bottom
                Topmost = ViewModel.IsNotificationWindowExplicitShowed;
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
                    DwmGetColorizationColor(out var color, out _);
                    var c = NativeWindowHelper.GetColor((int)color);
                    primary = secondary = c;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "获取系统主题色失败。");
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
        if (plan.TimeRule.WeekDay != (int)ExactTimeService.GetCurrentLocalDateTime().DayOfWeek)
        {
            return false;
        }

        var dd = ExactTimeService.GetCurrentLocalDateTime().Date - ViewModel.Settings.SingleWeekStartTime.Date;
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
        if (ViewModel.Profile.TempClassPlanSetupTime.Date < ExactTimeService.GetCurrentLocalDateTime().Date)  // 清除过期临时课表
        {
            ViewModel.Profile.TempClassPlanId = null;
        }

        // 检测是否启用课表加载
        if (!ViewModel.IsClassPlanEnabled)
        {
            ViewModel.CurrentClassPlan = null;
            return; 
        }
        // 加载临时层
        if (ViewModel.Profile.IsOverlayClassPlanEnabled && 
            ViewModel.Profile.OverlayClassPlanId != null &&
            ViewModel.Profile.ClassPlans.TryGetValue(ViewModel.Profile.OverlayClassPlanId, out var overlay))
        {
            ViewModel.CurrentClassPlan = overlay;
            return;
        }
        // 加载临时课表
        if (ViewModel.Profile.TempClassPlanId != null &&
            ViewModel.Profile.ClassPlans.TryGetValue(ViewModel.Profile.TempClassPlanId, out var tempClassPlan))
        {
            ViewModel.CurrentClassPlan = tempClassPlan;
            return;
        }
        // 加载课表
        var a = (from p in ViewModel.Profile.ClassPlans
            where CheckClassPlan(p.Value) && !p.Value.IsOverlay && p.Value.IsEnabled
            select p.Value)
            .ToList();
        ViewModel.CurrentClassPlan = a.FirstOrDefault();
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

    public void OpenSettingsWindow()
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
        UpdateTimer.Stop();
    }

    private void UpdateWindowPos(bool updateEffectWindow=false)
    {
        GetCurrentDpi(out var dpiX, out var dpiY);

        var scale = ViewModel.Settings.Scale;
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
        Width = screen.WorkingArea.Width / dpiX;
        //Height = GridRoot.ActualHeight * scale;
        Left = (screen.WorkingArea.Left + ox) / dpiX;

        switch (ViewModel.Settings.WindowDockingLocation)
        {
            case 0: //左上
                //Left = (screen.WorkingArea.Left + ox) / dpiX;
                Top = (screen.WorkingArea.Top + oy) / dpiY;
                break;
            case 1: // 中上
                //Left = (c - aw / 2 + ox) / dpiX;
                Top = (screen.WorkingArea.Top + oy) / dpiY;
                break;
            case 2: // 右上
                //Left = (screen.WorkingArea.Right - aw + ox) / dpiX;
                Top = (screen.WorkingArea.Top + oy) / dpiY;
                break;
            case 3: // 左下
                //Left = (screen.WorkingArea.Left + ox) / dpiX;
                Top = (screen.WorkingArea.Bottom - ah + oy) / dpiY;
                break;
            case 4: // 中下
                //Left = (c - aw / 2 + ox) / dpiX;
                Top = (screen.WorkingArea.Bottom - ah + oy) / dpiY;
                break;
            case 5: // 右下
                //Left = (screen.WorkingArea.Right - aw + ox) / dpiX;
                Top = (screen.WorkingArea.Bottom - ah + oy) / dpiY;
                break;
        }
        if (updateEffectWindow)
            TopmostEffectWindow.UpdateWindowPos(screen, 1 / dpiX);
    }

    public void GetCurrentDpi(out double dpiX, out double dpiY)
    {
        dpiX = _latestDpiX;
        dpiY = _latestDpiY;
        try
        {
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget == null) 
                return;
            _latestDpiX = dpiX = 1.0 * source.CompositionTarget.TransformToDevice.M11;
            _latestDpiY = dpiY = 1.0 * source.CompositionTarget.TransformToDevice.M22;
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "无法获取当前dpi");
        }
    }

    private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        //UpdateWindowPos();
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

    public void OpenProfileSettingsWindow()
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

    private void MenuItemClassSwap_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentClassPlan == null || ClassChangingWindow != null)
        {
            return;
        }

        ViewModel.IsBusy = true;
        ClassChangingWindow = new ClassChangingWindow()
        {
            ClassPlan = ViewModel.CurrentClassPlan
        };
        ClassChangingWindow.ShowDialog();
        ClassChangingWindow.DataContext = null;
        ClassChangingWindow = null;
        ViewModel.IsBusy = false;
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((HwndSource)PresentationSource.FromVisual(this)).AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
        {
            //想要让窗口透明穿透鼠标和触摸等，需要同时设置 WS_EX_LAYERED 和 WS_EX_TRANSPARENT 样式，
            //确保窗口始终有 WS_EX_LAYERED 这个样式，并在开启穿透时设置 WS_EX_TRANSPARENT 样式
            //但是WPF窗口在未设置 AllowsTransparency = true 时，会自动去掉 WS_EX_LAYERED 样式（在 HwndTarget 类中)，
            //如果设置了 AllowsTransparency = true 将使用WPF内置的低性能的透明实现，
            //所以这里通过 Hook 的方式，在不使用WPF内置的透明实现的情况下，强行保证这个样式存在。
            if (msg == (int)0x007C && (long)wParam == (long)WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE)
            {
                var styleStruct = (NativeWindowHelper.StyleStruct)Marshal.PtrToStructure(lParam, typeof(NativeWindowHelper.StyleStruct));
                styleStruct.styleNew |= (int)NativeWindowHelper.WS_EX_LAYERED;
                Marshal.StructureToPtr(styleStruct, lParam, false);
                handled = true;
            }
            return IntPtr.Zero;
        });
    }

    private void GridWrapper_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (double.IsNaN(e.NewSize.Width))
            return;
        if ( double.IsNaN(BackgroundWidth))
        {
            BackgroundWidth = e.NewSize.Width;
            return;
        }

        var m = e.NewSize.Width > BackgroundWidth;
        var s = ViewModel.Settings.DebugAnimationScale;
        var t = m ? 600 * s : 800 * s;
        var da = new DoubleAnimation()  
        {
            From = BackgroundWidth,
            To = e.NewSize.Width,
            Duration = new Duration(TimeSpan.FromMilliseconds(t)),
            EasingFunction = m ? new BackEase()
            {
                EasingMode = EasingMode.EaseOut,
                Amplitude = 0.4
            } : new BackEase()
            {
                EasingMode = EasingMode.EaseOut,
                Amplitude = 0.2
            }
        };
        var storyboard = new Storyboard()
        {
        };
        Storyboard.SetTarget(da, this);
        Storyboard.SetTargetProperty(da, new PropertyPath(BackgroundWidthProperty));
        storyboard.Children.Add(da);
        storyboard.Begin();
        storyboard.Completed += (o, args) =>
        {
            storyboard.Remove();
        };
    }

    private void TrayIconOnClicked_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        
    }
}
