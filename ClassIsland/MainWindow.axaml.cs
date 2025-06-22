using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Win32.UI.Accessibility;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data.Core;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Models.EventArgs;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using ClassIsland.Views;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using NAudio.Wave;
using Sentry;
using NAudio.Wave.SampleProviders;
using Linearstar.Windows.RawInput;
using YamlDotNet.Core;



#if DEBUG
using JetBrains.Profiler.Api;
#endif

namespace ClassIsland;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // public static readonly ICommand TrayIconLeftClickedCommand = new RoutedCommand();

    public event EventHandler? StartupCompleted;

    public MainViewModel ViewModel
    {
        get;
        set;
    }

    // private Storyboard NotificationProgressBar { get; set; } = new Storyboard();

    private SettingsService SettingsService
    {
        get;
    }

    private ITaskBarIconService TaskBarIconService
    {
        get;
    }

    private IThemeService ThemeService
    {
        get;
    }

    public INotificationHostService NotificationHostService
    {
        get;
    }

    public IProfileService ProfileService
    {
        get;
    }

    public ILessonsService LessonsService { get; }

    // public TopmostEffectWindow TopmostEffectWindow { get; }

    private bool IsRunningCompatibleMode { get; set; } = false;

    private Stopwatch UserPrefrenceUpdateStopwatch
    {
        get;
    } = new();

    private IExactTimeService ExactTimeService { get; }

    public ISpeechService SpeechService { get; }

    public IComponentsService ComponentsService { get; }

    private ILogger<MainWindow> Logger;

    private double _latestDpiX = 1.0;
    private double _latestDpiY = 1.0;

    private DispatcherTimer TouchInFadingTimer { get; set; } = new();

    private Stopwatch RawInputUpdateStopWatch { get; } = new();

    // public ClassChangingWindow? ClassChangingWindow { get; set; }
    
    private IUriNavigationService UriNavigationService { get; }
    public IRulesetService RulesetService { get; }
    public IWindowRuleService WindowRuleService { get; }
    public IManagementService ManagementService { get; }

    public event EventHandler<MousePosChangedEventArgs>? MousePosChanged;

    public event EventHandler<RawInputEventArgs>? RawInputEvent;

    public event EventHandler<MainWindowAnimationEventArgs>? MainWindowAnimationEvent;

    private Point _centerPointCache = new Point(0, 0);


    public static readonly StyledProperty<double> BackgroundWidthProperty = AvaloniaProperty.Register<MainWindow, double>(
        nameof(BackgroundWidth));

    public double BackgroundWidth
    {
        get => GetValue(BackgroundWidthProperty);
        set => SetValue(BackgroundWidthProperty, value);
    }

    public static readonly StyledProperty<double> NotificationProgressBarValueProperty = AvaloniaProperty.Register<MainWindow, double>(
        nameof(NotificationProgressBarValue));
    public double NotificationProgressBarValue
    {
        get => GetValue(NotificationProgressBarValueProperty);
        set => SetValue(NotificationProgressBarValueProperty, value);
    
    }

    public MainWindow(SettingsService settingsService, 
        IProfileService profileService,
        INotificationHostService notificationHostService, 
        ITaskBarIconService taskBarIconService,
        IThemeService themeService, 
        ILogger<MainWindow> logger, 
        ISpeechService speechService,
        IExactTimeService exactTimeService,
        IComponentsService componentsService,
        ILessonsService lessonsService,
        IUriNavigationService uriNavigationService,
        IRulesetService rulesetService,
        IWindowRuleService windowRuleService,
        IManagementService managementService)
    {
        Logger = logger;
        SpeechService = speechService;
        SettingsService = settingsService;
        TaskBarIconService = taskBarIconService;
        NotificationHostService = notificationHostService;
        ThemeService = themeService;
        ProfileService = profileService;
        ExactTimeService = exactTimeService;
        // TopmostEffectWindow = topmostEffectWindow;
        ComponentsService = componentsService;
        LessonsService = lessonsService;
        UriNavigationService = uriNavigationService;
        RulesetService = rulesetService;
        WindowRuleService = windowRuleService;
        ManagementService = managementService;

        DataContext = this;

        IAppHost.GetService<ISplashService>().SetDetailedStatus("正在初始化主界面（步骤 1/2）");
        SettingsService.PropertyChanged += (sender, args) =>
        {
            LoadSettings();
        };
        LessonsService.PreMainTimerTicked += LessonsServiceOnPreMainTimerTicked;
        LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        ViewModel = new MainViewModel();
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        InitializeComponent();
        RulesetService.StatusUpdated += RulesetServiceOnStatusUpdated;
        TouchInFadingTimer.Tick += TouchInFadingTimerOnTick;
        IsRunningCompatibleMode = SettingsService.Settings.IsCompatibleWindowTransparentEnabled;
    }

    private void TouchInFadingTimerOnTick(object? sender, EventArgs e)
    {
        ViewModel.IsMouseIn = false;
        TouchInFadingTimer.Stop();
    }

    private void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        if (ViewModel.Settings.HideMode == 1)
        {
            ViewModel.IsHideRuleSatisfied = RulesetService.IsRulesetSatisfied(ViewModel.Settings.HiedRules);
        }
        // Detect fullscreen
        var screen = GetSelectedScreenSafe();
        if (screen != null)
        {
            ViewModel.IsForegroundFullscreen = NativeWindowHelper.IsForegroundFullScreen(screen);
            ViewModel.IsForegroundMaxWindow = NativeWindowHelper.IsForegroundMaxWindow(screen);
        }
    }

    private async void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        // 处理提醒请求队列
        await ProcessNotification();
    }

    private void LessonsServiceOnPreMainTimerTicked(object? sender, EventArgs e)
    {
        //SettingsService.Settings.IsNetworkConnect = InternetGetConnectedState(out var _);
        //SettingsService.Settings.IsNotificationSpeechEnabled = SettingsService.Settings.IsNetworkConnect || SettingsService.Settings.IsSystemSpeechSystemExist;
        if (SettingsService.Settings.IsMainWindowDebugEnabled)
            ViewModel.DebugCurrentTime = ExactTimeService.GetCurrentLocalDateTime();

        UpdateWindowPos(true);
        if (ViewModel.Settings.WindowLayer == 0)
        {
            //SetBottom();
        }
        //NotificationHostService.OnUpdateTimerTick(this, EventArgs.Empty);

        if (SettingsService.Settings.DebugTimeSpeed != 0)
        {
            SettingsService.Settings.DebugTimeOffsetSeconds += (SettingsService.Settings.DebugTimeSpeed - 1) * 0.05;
        }
    }

    private void BeginStoryboardInLine(string name)
    {
        ViewModel.LastStoryboardName = name;
        MainWindowAnimationEvent?.Invoke(this, new MainWindowAnimationEventArgs(name));
    }

    private void UpdateMouseStatus()
    {
        try
        {
            GetCursorPos(out var ptr);
            MousePosChanged?.Invoke(this, new MousePosChangedEventArgs(ptr));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "无法更新鼠标状态。");
        }
    }

    [Obsolete]
    private bool GetMouseStatusByPos(System.Drawing.Point ptr)
    {
        return false;
    }

    public Point GetCenter()
    {
        GetCurrentDpi(out var dpi, out _);
        
        if (this.Find<Grid>("PART_GridWrapper") is not { } gridWrapper) 
            return _centerPointCache;  // 在切换组件配置时可能出现找不到 GridWrapper 的情况，此时要使用上一次的数值
        var p = gridWrapper.TranslatePoint(new Point(gridWrapper.Bounds.Width / 2, gridWrapper.Bounds.Height / 2), this);
        if (p == null)
        {
            return _centerPointCache;
        }
        return _centerPointCache = new Point(p.Value.X, Bounds.Top + (Bounds.Height / 2));
    }

    private void PreProcessNotificationContent(NotificationContent content)
    {
        if (content.EndTime != null)  // 如果目标结束时间为空，那么就计算持续时间
        {
            var rawTime = content.EndTime.Value - ExactTimeService.GetCurrentLocalDateTime();
            content.Duration = rawTime > TimeSpan.Zero ? rawTime : TimeSpan.Zero;
        }
    }

    private async Task ProcessNotification()
    {
        if (ViewModel.IsOverlayOpened)
        {
            return;
        }
        ViewModel.IsOverlayOpened = true;  // 上锁

        var notificationsShowed = false;

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
            INotificationSettings settings = ViewModel.Settings;
            foreach (var i in new List<NotificationSettings?>([request.ChannelSettings, request.ProviderSettings, request.RequestNotificationSettings]).OfType<NotificationSettings>().Where(i => i.IsSettingsEnabled))
            {
                settings = i;
                break;
            }
            var mask = request.MaskContent;
            var overlay = request.OverlayContent;
            var isMaskSpeechEnabled = settings.IsSpeechEnabled && request.MaskContent.IsSpeechEnabled && ViewModel.Settings.AllowNotificationSpeech;
            var isOverlaySpeechEnabled = request.OverlayContent != null && settings.IsSpeechEnabled && request.OverlayContent.IsSpeechEnabled && ViewModel.Settings.AllowNotificationSpeech;
            Logger.LogInformation("处理通知请求：{} {}", request.MaskContent.GetType(), request.OverlayContent?.GetType());
            var cancellationToken = request.CancellationTokenSource.Token;

            PreProcessNotificationContent(mask);


            if (request.MaskContent.Duration > TimeSpan.Zero && !cancellationToken.IsCancellationRequested)
            {
                notificationsShowed = true;
                ViewModel.CurrentMaskContent = request.MaskContent;  // 加载Mask元素
                ViewModel.IsNotificationWindowExplicitShowed = settings.IsNotificationTopmostEnabled && ViewModel.Settings.AllowNotificationTopmost;
                if (ViewModel.IsNotificationWindowExplicitShowed && ViewModel.Settings.WindowLayer == 0)  // 如果处于置底状态，还需要激活窗口来强制显示窗口。
                {
                    UpdateWindowLayer();
                    ReCheckTopmostState();
                }

                if (isMaskSpeechEnabled)
                {
                    SpeechService.EnqueueSpeechQueue(request.MaskContent.SpeechContent);
                }
                BeginStoryboardInLine("OverlayMaskIn");
                // 播放提醒音效
                if (settings.IsNotificationSoundEnabled && ViewModel.Settings.AllowNotificationSound)
                {
                    try
                    {
                        var provider = string.IsNullOrWhiteSpace(settings.NotificationSoundPath)
                            ? new StreamMediaFoundationReader(
                                AssetLoader.Open(INotificationProvider.DefaultNotificationSoundUri)).ToSampleProvider()
                            : new AudioFileReader(settings.NotificationSoundPath);
                        var volume = new VolumeSampleProvider(provider)
                        {
                            Volume = (float)SettingsService.Settings.NotificationSoundVolume
                        };
                        player.Init(volume);
                        player.Play();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "无法播放提醒音效：{}", settings.NotificationSoundPath);
                    }
                }
                // 播放提醒特效
                if (settings.IsNotificationEffectEnabled && ViewModel.Settings.AllowNotificationEffect &&
                    GridRoot.IsVisible && ViewModel.Settings.IsMainWindowVisible && !IsRunningCompatibleMode)
                {
                    var center = GetCenter();
                    // TopmostEffectWindow.Dispatcher.Invoke(() =>
                    // {
                    //     TopmostEffectWindow.PlayEffect(new RippleEffect()
                    //     {
                    //         CenterX = center.X,
                    //         CenterY = center.Y
                    //     });
                    // });
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Run(() => cancellationToken.WaitHandle.WaitOne(request.MaskContent.Duration), cancellationToken);
                }
                if (overlay is null || cancellationToken.IsCancellationRequested || overlay.Duration <= TimeSpan.Zero)
                {
                    BeginStoryboardInLine("OverlayMaskOutDirect");
                }
                else
                {
                    PreProcessNotificationContent(overlay);
                    ViewModel.CurrentOverlayContent = overlay;
                    if (isOverlaySpeechEnabled)
                    {
                        SpeechService.EnqueueSpeechQueue(overlay.SpeechContent);
                    }
                    BeginStoryboardInLine("OverlayMaskOut");
                    ViewModel.OverlayRemainStopwatch.Restart();
                    // 倒计时动画
                    // var da = new DoubleAnimation()
                    // {
                    //     From = 1.0,
                    //     To = 0.0,
                    //     Duration = new Duration(overlay.Duration),
                    // };
                    // var storyboard = new Storyboard()
                    // {
                    // };
                    // Storyboard.SetTarget(da, this);
                    // Storyboard.SetTargetProperty(da, new PropertyPath(NotificationProgressBarValueProperty));
                    // storyboard.Children.Add(da);
                    // storyboard.Begin();
                    // if (!cancellationToken.IsCancellationRequested)
                    // {
                    //     await Task.Run(() => cancellationToken.WaitHandle.WaitOne(overlay.Duration),
                    //         cancellationToken);
                    // }
                    // storyboard.Stop();
                    ViewModel.OverlayRemainStopwatch.Stop();
                }
                SpeechService.ClearSpeechQueue();
            }

            if (NotificationHostService.RequestQueue.Count < 1 && notificationsShowed)
            {
                BeginStoryboardInLine("OverlayOut");
            }
            await request.CompletedTokenSource.CancelAsync();
        }

        ViewModel.CurrentOverlayContent = null;
        ViewModel.CurrentMaskContent = null;
        ViewModel.IsOverlayOpened = false;
        if (ViewModel.IsNotificationWindowExplicitShowed)
        {
            ViewModel.IsNotificationWindowExplicitShowed = false;
            SetBottom();
            UpdateWindowLayer();
        }
    }

    public override void Show()
    {
        IAppHost.GetService<ISplashService>().SetDetailedStatus("正在加载界面主题（2）");
        UpdateTheme();
        IAppHost.GetService<IXamlThemeService>().LoadAllThemes();
        IAppHost.GetService<ISplashService>().SetDetailedStatus("正在初始化托盘菜单");
        TaskBarIconService.MainTaskBarIcon.Menu = this.FindResource("AppMenu") as NativeMenu;
        TaskBarIconService.MainTaskBarIcon.IsVisible = true;
        ViewModel.OverlayRemainTimePercents = 0.5;
        WindowRuleService.ForegroundWindowChanged += WindowRuleServiceOnForegroundWindowChanged;
        DiagnosticService.EndStartup();

        if (!ViewModel.Settings.IsNotificationEffectRenderingScaleAutoSet)
        {
            AutoSetNotificationEffectRenderingScale();
        }

        if (!ViewModel.Settings.IsWelcomeWindowShowed)
        {
            if (ViewModel.Settings.IsSplashEnabled)
            {
                App.GetService<ISplashService>().EndSplash();
            }
            // TODO: welcome window
            // var w = new WelcomeWindow()
            // {
            //     ViewModel =
            //     {
            //         Settings = ViewModel.Settings
            //     }
            // };
            // var r = w.ShowDialog();
            // if (r == false)
            // {
            //     ViewModel.IsClosing = true;
            //     Close();
            // }
            else
            {
                ViewModel.Settings.IsWelcomeWindowShowed = true;
            }
        }

        UriNavigationService.HandleAppNavigation("class-swap", args => OpenClassSwapWindow());

        IAppHost.GetService<ISplashService>().SetDetailedStatus("正在初始化输入");
        if (SettingsService.Settings.UseRawInput)
        {
            try
            {
                InitializeRawInputHandler();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "无法初始化 RawInput，已回退到兼容模式。");
                LessonsService.PreMainTimerTicked += ProcessMousePos;
                ViewModel.Settings.IsErrorLoadingRawInput = true;
            }
        }
        else
        {
            LessonsService.PreMainTimerTicked += ProcessMousePos;
        }

        StartupCompleted?.Invoke(this, EventArgs.Empty);

        if (!string.IsNullOrWhiteSpace(App.ApplicationCommand.Uri))
        {
            try
            {
                UriNavigationService.NavigateWrapped(new Uri(App.ApplicationCommand.Uri));
            }
            catch (Exception ex)
            {
                // ignored
            }
        }
#if DEBUG
        MemoryProfiler.GetSnapshot("MainWindow OnContentRendered");
#endif
        base.Show();
    }

    private void WindowRuleServiceOnForegroundWindowChanged(HWINEVENTHOOK hwineventhook, uint @event, HWND hwnd, int idobject, int idchild, uint ideventthread, uint dwmseventtime)
    {
        //if (@event is not (EVENT_SYSTEM_FOREGROUND))
        //{
        //    return;
        //}

        //ReCheckTopmostState();
    }

    private void ReCheckTopmostState()
    {
        var handle = TryGetPlatformHandle()?.Handle ?? nint.Zero;
        if (ViewModel.IsNotificationWindowExplicitShowed || ViewModel.Settings.WindowLayer == 1)
        {
            SetWindowPos((HWND)handle, NativeWindowHelper.HWND_TOPMOST, 0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOSENDCHANGING);
            //Topmost = true;
        }
    }

    private void InitializeRawInputHandler()
    {
        var handle = TryGetPlatformHandle()?.Handle ?? nint.Zero;
        RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse,
            RawInputDeviceFlags.InputSink, handle);
        RawInputDevice.RegisterDevice(HidUsageAndPage.TouchScreen,
            RawInputDeviceFlags.InputSink, handle);

        RawInputUpdateStopWatch.Start();
        Win32Properties.AddWndProcHookCallback(this, ProcWnd);
    }

    private void ProcessMousePos(object? sender, EventArgs e)
    {
        UpdateMouseStatus();
    }

    private IntPtr ProcWnd(IntPtr hwnd, uint msg, IntPtr param, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x00FF) // WM_INPUT
        {
            if (RawInputUpdateStopWatch.ElapsedMilliseconds < 20)
            {
                return IntPtr.Zero;
            }
            RawInputUpdateStopWatch.Restart();
            // Create an RawInputData from the handle stored in lParam.
            var data = RawInputData.FromHandle(lParam);
            RawInputEvent?.Invoke(this, new RawInputEventArgs(data));
        }

        if (msg == 0x0047) // WM_WINDOWPOSCHANGED
        {
            var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
            Logger.LogTrace("WM_WINDOWPOSCHANGED {}", pos.flags);
            if ((pos.flags & SET_WINDOW_POS_FLAGS.SWP_NOZORDER) == 0) // SWP_NOZORDER
            {
                Logger.LogTrace("Z order changed");
                if (pos.hwndInsertAfter != NativeWindowHelper.HWND_TOPMOST)
                {
                    ReCheckTopmostState();
                }

                if (pos.hwndInsertAfter != NativeWindowHelper.HWND_BOTTOM)
                {
                    SetBottom();
                }
            }
        }

        return nint.Zero;
    }

    private void AutoSetNotificationEffectRenderingScale()
    {
        var screen = GetSelectedScreenSafe();
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

    internal Screen? GetSelectedScreenSafe()
    {
        return ViewModel.Settings.WindowDockingMonitorIndex < Screens.ScreenCount 
               && ViewModel.Settings.WindowDockingMonitorIndex >= 0
            ? Screens.All[ViewModel.Settings.WindowDockingMonitorIndex]
            : Screens.Primary;
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
        ViewModel.Settings.PropertyChanged += SettingsOnPropertyChanged;
    }

    public void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateTheme();
        if (e.PropertyName is nameof(ViewModel.Settings.IsMouseInFadingReversed)
                           or nameof(ViewModel.Settings.IsMouseInFadingEnabled))
        {
            UpdateFadeStatus();
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsMouseIn))
        {
            UpdateFadeStatus();
        }
    }

    private void UpdateFadeStatus()
    {
        ViewModel.IsMainWindowFaded =
            ViewModel.Settings.IsMouseInFadingEnabled &&
           (ViewModel.IsMouseIn ^ ViewModel.Settings.IsMouseInFadingReversed);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        var span = SentrySdk.GetSpan()?.StartChild("startup-initialize-mainWindow");
        ViewModel.Profile.PropertyChanged += (sender, args) => SaveProfile();
        ViewModel.Settings.PropertyChanged += SettingsOnPropertyChanged;
        LoadSettings();
        //ViewModel.CurrentProfilePath = ViewModel.Settings.SelectedProfile;
        LoadProfile();
        IAppHost.GetService<ISplashService>().SetDetailedStatus("正在加载界面主题（1）");
        // UpdateTheme();
        UserPrefrenceUpdateStopwatch.Start();
        SystemEvents.UserPreferenceChanged += OnSystemEventsOnUserPreferenceChanged;
        AppBase.Current.AppStopping += (sender, args) => SystemEvents.UserPreferenceChanged -= OnSystemEventsOnUserPreferenceChanged;
        span?.Finish();
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
        var hWnd = (HWND)TryGetPlatformHandle()!.Handle;
        if (ViewModel.Settings.WindowLayer != 0)
        {
            return;
        }
        if (ViewModel.IsNotificationWindowExplicitShowed)
        {
            //SetWindowPos(hWnd, NativeWindowHelper.HWND_TOPMOST, 0, 0, 0, 0,
            //    SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
            return;
        }
        SetWindowPos(hWnd, NativeWindowHelper.HWND_BOTTOM, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    private async void UpdateTheme()
    {
        UpdateWindowPos();
        var hWnd = (HWND)TryGetPlatformHandle()!.Handle;
        SetLayeredWindowAttributes(hWnd, new COLORREF(0), 255, (LAYERED_WINDOW_ATTRIBUTES_FLAGS)0x2);
        var style = GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        style |= NativeWindowHelper.WS_EX_TOOLWINDOW;
        if (!ViewModel.Settings.IsMouseClickingEnabled)
        {
            var r = SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style | (int)WINDOW_EX_STYLE.WS_EX_LAYERED | (int)WINDOW_EX_STYLE.WS_EX_TRANSPARENT);
        }
        else
        {
            style &= ~NativeWindowHelper.WS_EX_TRANSPARENT;
            var r = SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style);
        }

        UpdateWindowLayer();

        var primary = Colors.DodgerBlue;
        var secondary = Colors.DodgerBlue;
        switch (ViewModel.Settings.ColorSource)
        {
            case 0: //custom
                primary = ViewModel.Settings.PrimaryColor;
                secondary = ViewModel.Settings.SecondaryColor;
                break;
            case 1: // 壁纸主题色
            case 3: // 屏幕主题色
                primary = secondary = ViewModel.Settings.SelectedPlatte;
                break;
            case 2:
                try
                {
                    // DwmGetColorizationColor(out var color, out _);
                    // var c = NativeWindowHelper.GetColor((int)color);
                    // primary = secondary = c;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "获取系统主题色失败。");
                }
                break;
        }
        ThemeService.SetTheme(ViewModel.Settings.Theme, primary);

        ResourceLoaderBorder.Resources[nameof(SettingsService.Settings.MainWindowSecondaryFontSize)] =
            SettingsService.Settings.MainWindowSecondaryFontSize;
        ResourceLoaderBorder.Resources[nameof(SettingsService.Settings.MainWindowBodyFontSize)] =
            SettingsService.Settings.MainWindowBodyFontSize;
        ResourceLoaderBorder.Resources[nameof(SettingsService.Settings.MainWindowEmphasizedFontSize)] =
            SettingsService.Settings.MainWindowEmphasizedFontSize;
        ResourceLoaderBorder.Resources[nameof(SettingsService.Settings.MainWindowLargeFontSize)] =
            SettingsService.Settings.MainWindowLargeFontSize;

        if (ViewModel.Settings.IsCustomForegroundColorEnabled)
        {
            var brush = new SolidColorBrush(ViewModel.Settings.CustomForegroundColor);
            ResourceLoaderBorder.SetValue(ForegroundProperty, brush);
            ResourceLoaderBorder.SetValue(TextElement.ForegroundProperty, brush);
            ResourceLoaderBorder.Resources["MaterialDesignBody"] = brush;
        }
        else
        {
            if (ResourceLoaderBorder.Resources.ContainsKey("MaterialDesignBody"))
            {
                ResourceLoaderBorder.Resources.Remove("MaterialDesignBody");
            }
            ResourceLoaderBorder.SetValue(ForegroundProperty, AvaloniaProperty.UnsetValue);
            ResourceLoaderBorder.SetValue(TextElement.ForegroundProperty, AvaloniaProperty.UnsetValue);
        }

        App._isCriticalSafeModeEnabled = ViewModel.Settings.IsCriticalSafeMode;
    }

    private void UpdateWindowLayer()
    {
        switch (ViewModel.Settings.WindowLayer)
        {
            case 0: // bottom
                Topmost = ViewModel.IsNotificationWindowExplicitShowed;
                break;
            case 1:
                Topmost = true;
                break;
        }
    }

    private void ButtonSettings_OnClick(object sender, EventArgs e)
    {
        OpenProfileSettingsWindow();
    }

    private void ButtonResizeDebug_OnClick(object sender, RoutedEventArgs e)
    {
        SizeToContent = SizeToContent.WidthAndHeight;
    }

    private void MenuItemSettings_OnClick(object sender, EventArgs e)
    {
        App.GetService<SettingsWindowNew>().Open();
    }


    private async void MenuItemExitApp_OnClick(object sender, EventArgs e)
    {
        if (!await ManagementService.AuthorizeByLevel(ManagementService.CredentialConfig.ExitApplicationAuthorizeLevel))
        {
            return;
        }
        
        ViewModel.IsClosing = true;
        Close();
        AppBase.Current.Stop();
    }
    private void MenuItemRestartApp_OnClick(object sender, EventArgs e)
    {
        AppBase.Current.Restart();
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (!ViewModel.IsClosing)
        {
            e.Cancel = true;
            return;
        }

    }

    private void UpdateWindowPos(bool updateEffectWindow=false)
    {
        GetCurrentDpi(out var dpiX, out var dpiY);

        var scale = ViewModel.Settings.Scale;
        ViewModel.GridRootLeft = Width / 10 * (scale - 1);
        ViewModel.GridRootTop = Height / 10 * (scale - 1);

        var screen = GetSelectedScreenSafe();
        if (screen == null)
            return;
        double offsetAreaTop = ViewModel.Settings.IsIgnoreWorkAreaEnabled ? screen.Bounds.Y : screen.WorkingArea.Y;
        double offsetAreaBottom = ViewModel.Settings.IsIgnoreWorkAreaEnabled ? screen.Bounds.Bottom : screen.WorkingArea.Bottom;
        var aw = Bounds.Width * dpiX;
        var ah = Bounds.Height * dpiY;
        var c = (double)(screen.WorkingArea.X + screen.WorkingArea.Right) / 2;
        var ox = ViewModel.Settings.WindowDockingOffsetX;
        var oy = ViewModel.Settings.WindowDockingOffsetY;
        Width = screen.WorkingArea.Width / dpiX;
        //Height = GridRoot.ActualHeight * scale;
        var x = (screen.WorkingArea.X + ox) / dpiX;
        var y = ViewModel.Settings.WindowDockingLocation switch
        {
            0 => //左上
                //Left = (screen.WorkingArea.Left + ox) / dpiX;
                (offsetAreaTop + oy) / dpiY,
            1 => // 中上
                //Left = (c - aw / 2 + ox) / dpiX;
                (offsetAreaTop + oy) / dpiY,
            2 => // 右上
                //Left = (screen.WorkingArea.Right - aw + ox) / dpiX;
                (offsetAreaTop + oy) / dpiY,
            3 => // 左下
                //Left = (screen.WorkingArea.Left + ox) / dpiX;
                (offsetAreaBottom - ah + oy) / dpiY,
            4 => // 中下
                //Left = (c - aw / 2 + ox) / dpiX;
                (offsetAreaBottom - ah + oy) / dpiY,
            5 => // 右下
                //Left = (screen.WorkingArea.Right - aw + ox) / dpiX;
                (offsetAreaBottom - ah + oy) / dpiY,
            _ => 0.0
        };
        Position = new PixelPoint((int)x, (int)y);

        // if (updateEffectWindow)
        // {
        //     TopmostEffectWindow.Dispatcher.Invoke(() =>
        //     {
        //         TopmostEffectWindow.UpdateWindowPos(screen, 1 / dpiX);
        //     });
        // }
    }

    public void GetCurrentDpi(out double dpiX, out double dpiY, Visual? visual=null)
    {
        dpiX = _latestDpiX;
        dpiY = _latestDpiY;
        if (ViewModel.IsClosing)
        {
            return;
        }
        try
        {
            var screen = Screens.ScreenFromWindow(this);
            dpiX = screen?.Scaling ?? 1.0;
            dpiY = screen?.Scaling ?? 1.0;
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

    private void MenuItemTemporaryClassPlan_OnClick(object sender, EventArgs e)
    {
        // App.GetService<ProfileSettingsWindow>().OpenDrawer("TemporaryClassPlan");
        OpenProfileSettingsWindow();
    }

    public void OpenProfileSettingsWindow()
    {
        // App.GetService<ProfileSettingsWindow>().Open();
    }

    private void MenuItemAbout_OnClick(object sender, RoutedEventArgs e)
    {
        // App.GetService<SettingsWindowNew>().Open("about");
    }

    private void MenuItemDebugWelcomeWindow_OnClick(object sender, RoutedEventArgs e)
    {
        // var ww = new WelcomeWindow();
        // ww.ShowDialog();
    }

    private void MenuItemDebugWelcomeWindow2_OnClick(object sender, EventArgs e)
    {
        ViewModel.Settings.IsWelcomeWindowShowed = false;
    }

    private void MenuItemHelps_OnClick(object sender, EventArgs e)
    {
        UriNavigationService.Navigate(new Uri("https://docs.classisland.tech/app/"));
    }

    private void MenuItemUpdates_OnClick(object sender, EventArgs e)
    {
        // App.GetService<SettingsWindowNew>().Open("update");
    }

    private void GridRoot_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Width = e.NewSize.Width * ViewModel.Settings.Scale;
        Height = e.NewSize.Height * ViewModel.Settings.Scale;
    }

    private async void MenuItemDebugFitSize_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OverlayRemainTimePercents = 0.5;
    }

    private void MenuItemClearAllNotifications_OnClick(object sender, EventArgs e)
    {
        NotificationHostService.CancelAllNotifications();
    }

    private void MenuItemNotificationSettings_OnClick(object sender, RoutedEventArgs e)
    {
        // App.GetService<SettingsWindowNew>().Open("notification");
    }

    private void MenuItemSwitchMainWindowVisibility_OnClick(object sender, EventArgs e)
    {
        ViewModel.Settings.IsMainWindowVisible = !ViewModel.Settings.IsMainWindowVisible;
    }

    private void MenuItemClassSwap_OnClick(object sender, EventArgs e)
    {
        OpenClassSwapWindow();
    }

    private async void OpenClassSwapWindow()
    {
        if (!await ManagementService.AuthorizeByLevel(ManagementService.CredentialConfig.ChangeLessonsAuthorizeLevel))
        {
            return;
        }
        if (LessonsService.CurrentClassPlan == null) // 如果今天没有课程，则选择临时课表
        {
            // App.GetService<ProfileSettingsWindow>().OpenDrawer("TemporaryClassPlan");
            OpenProfileSettingsWindow();
            return;
        }

        // if (ClassChangingWindow != null)
        // {
        //     return;
        // }

        // ViewModel.IsBusy = true;
        // ClassChangingWindow = new ClassChangingWindow()
        // {
        //     ClassPlan = LessonsService.CurrentClassPlan
        // };
        // ClassChangingWindow.ShowDialog();
        // ClassChangingWindow.DataContext = null;
        // ClassChangingWindow = null;
        // ViewModel.IsBusy = false;
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        
    }

    private void MenuItemSettingsWindow2_OnClick(object sender, RoutedEventArgs e)
    {
        // IAppHost.GetService<SettingsWindowNew>().Open();
    }

    private void NativeMenuItemDebugDevTools_OnClick(object? sender, EventArgs e)
    {
        RaiseEvent(new KeyEventArgs()
        {
            Key = Key.F12,
            RoutedEvent = KeyDownEvent
        });
    }

    private async void NativeMenuItemDebugEnableTempClassPlan_OnClick(object? sender, EventArgs e)
    {
        var input = new TextBox();
        var dialog = new TaskDialog()
        {
            Header = "启用临时课表",
            Content = new StackPanel()
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock()
                    {
                        Text = $"输入课表 GUID，当前档案为 {ProfileService.CurrentProfilePath}"
                    },
                    input
                }
            },
            XamlRoot = this,
            Buttons =
            {
                TaskDialogButton.OKButton
            }
        };
        await dialog.ShowAsync();

        ProfileService.Profile.TempClassPlanId = input.Text;
        ProfileService.Profile.TempClassPlanSetupTime = ExactTimeService.GetCurrentLocalDateTime();
    }

    private void NativeMenuItemDebugCrashTest_OnClick(object? sender, EventArgs e)
    {
        var window = new CrashWindow();
        window.Show();
    }
}
