using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Data.Core;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Helpers;
using ClassIsland.Models.EventArgs;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Enums;
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

using Sentry;
using Linearstar.Windows.RawInput;
using YamlDotNet.Core;



#if DEBUG
using JetBrains.Profiler.Api;
#endif

namespace ClassIsland;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[PseudoClasses(":dock-top", ":dock-bottom")]
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
    
    private TopmostEffectWindow TopmostEffectWindow { get; }

    public event EventHandler<MousePosChangedEventArgs>? MousePosChanged;

    public event EventHandler<RawInputEventArgs>? RawInputEvent;

    public event EventHandler<MainWindowAnimationEventArgs>? MainWindowAnimationEvent;

    private Point _centerPointCache = new Point(0, 0);

    private List<object> TopmostLocks { get; } = [];


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

    public const string DefaultFontFamilyKey =
        "avares://ClassIsland/Assets/Fonts/#HarmonyOS Sans SC, avares://ClassIsland.Core/Assets/Fonts/#FluentSystemIcons-Resizable";
    
    public static FontFamily DefaultFontFamily { get; }=
        FontFamily.Parse(DefaultFontFamilyKey);

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
        IManagementService managementService,
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
        ComponentsService = componentsService;
        LessonsService = lessonsService;
        UriNavigationService = uriNavigationService;
        RulesetService = rulesetService;
        WindowRuleService = windowRuleService;
        ManagementService = managementService;

        DataContext = this;
        
        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.Antialias);
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

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
            ViewModel.IsHideRuleSatisfied = RulesetService.IsRulesetSatisfied(ViewModel.Settings.HideRules);
        }
        // Detect fullscreen
        var screen = GetSelectedScreenSafe();
        if (screen != null)
        {
            ViewModel.IsForegroundFullscreen = PlatformServices.WindowPlatformService.IsForegroundWindowFullscreen(screen);
            ViewModel.IsForegroundMaxWindow = PlatformServices.WindowPlatformService.IsForegroundWindowMaximized(screen);
        }
    }

    private async void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
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
            var ptr = PlatformServices.WindowPlatformService.GetMousePos();
            MousePosChanged?.Invoke(this, new MousePosChangedEventArgs(ptr));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "无法更新鼠标状态。");
        }
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

    private void PostInit()
    {
        IAppHost.GetService<ISplashService>().SetDetailedStatus("正在初始化托盘菜单");
        var menu = this.FindResource("AppMenu") as NativeMenu;
        TaskBarIconService.MainTaskBarIcon.Menu = menu;
        TaskBarIconService.MainTaskBarIcon.IsVisible = true;
        TaskBarIconService.MainTaskBarIcon.Clicked += MainTaskBarIconOnClicked;
        ViewModel.OverlayRemainTimePercents = 0.5;
        DiagnosticService.EndStartup();

        if (!ViewModel.Settings.IsNotificationEffectRenderingScaleAutoSet)
        {
            AutoSetNotificationEffectRenderingScale();
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

        ComponentPresenter.SetIsMainWindowLoaded(this, true);
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
    }

    public override void Show()
    {
        IAppHost.GetService<IXamlThemeService>().LoadAllThemes();
        PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.SkipManagement,
            ViewModel.Settings.WindowLayer == 1 || ViewModel.IsNotificationWindowExplicitShowed);
        IAppHost.GetService<ISplashService>().SetDetailedStatus("正在加载界面主题（2）");
        UpdateTheme();
        base.Show();
        UpdateWindowPos();
        Dispatcher.UIThread.InvokeAsync(PostInit, DispatcherPriority.ApplicationIdle);
    }

    private void MainTaskBarIconOnClicked(object? sender, EventArgs e)
    {
        switch (SettingsService.Settings.TaskBarIconClickBehavior)
        {
            case 0:
                if (!OperatingSystem.IsWindows())
                {
                    break;
                }
                // Get this tray icon's implementation
                ITrayIconImpl? impl = (ITrayIconImpl?)typeof(TrayIcon)
                    .GetProperty ("Impl", BindingFlags.NonPublic | BindingFlags.Instance)?
                    .GetValue (TaskBarIconService.MainTaskBarIcon);

                // Get the Windows tray icon implementation type
                Type? type = AppDomain.CurrentDomain.GetAssemblies ()
                    .Where (a => a.FullName?.StartsWith ("Avalonia.Win32") ?? false)
                    .SelectMany (a => a.GetTypes ())
                    .FirstOrDefault (t => t.Name == "TrayIconImpl");

                // If the Implementation and type are not null
                if (impl != null && type != null)
                {
                    // Get the OnRightClicked method
                    MethodInfo? methodInfo = type.GetMethod("OnRightClicked",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    // Invoke the method on the implementation
                    methodInfo?.Invoke(impl, null);
                }

                break;
            case 1:
                OpenProfileSettingsWindow();
                break;
            case 2:
                SettingsService.Settings.IsMainWindowVisible = !SettingsService.Settings.IsMainWindowVisible;
                break;
            case 3:
                OpenClassSwapWindow();
                break;
        }
    }

    private void ReCheckTopmostState()
    {
        var handle = TryGetPlatformHandle()?.Handle ?? nint.Zero;
        if (ViewModel.IsNotificationWindowExplicitShowed || ViewModel.Settings.WindowLayer == 1)
        {
            PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Topmost, true);
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
        if (!OperatingSystem.IsWindows())
        {
            return nint.Zero;
        } 
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
            var pos = Marshal.PtrToStructure<NativeWindowHelper.WINDOWPOS>(lParam);
            Logger.LogTrace("WM_WINDOWPOSCHANGED {}", pos.flags);
            if ((pos.flags & NativeWindowHelper.SWP_NOZORDER) == 0) // SWP_NOZORDER
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
        if (!CheckAccess())
        {
            return;
        }
        UpdateTheme();
        UpdateStyleStates();
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
        AppBase.Current.PlatformSettings!.ColorValuesChanged += OnSystemEventsOnUserPreferenceChanged;
        AppBase.Current.AppStopping += (sender, args) => AppBase.Current.PlatformSettings!.ColorValuesChanged -= OnSystemEventsOnUserPreferenceChanged;
        span?.Finish();
    }

    private void OnSystemEventsOnUserPreferenceChanged(object? sender, PlatformColorValues args)
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
        if (ViewModel.IsNotificationWindowExplicitShowed)
        {
            //SetWindowPos(hWnd, NativeWindowHelper.HWND_TOPMOST, 0, 0, 0, 0,
            //    SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
            return;
        }
        PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Bottommost, true);
    }
    
    private void UpdateStyleStates()
    {
        PseudoClasses.Set(":dock-top", ViewModel.Settings.WindowDockingLocation is 0 or 1 or 2);
        PseudoClasses.Set(":dock-bottom", ViewModel.Settings.WindowDockingLocation is 3 or 4 or 5);
    }

    private async void UpdateTheme()
    {
        UpdateWindowPos();
        PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.ToolWindow, true);
        if (!ViewModel.Settings.IsMouseClickingEnabled)
        {
            PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Transparent, true);
        }
        else
        {
            PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Transparent, false);
        }

        UpdateWindowLayer();

        var primary = (Color?)Colors.DodgerBlue;
        switch (ViewModel.Settings.ColorSource)
        {
            case 0: //custom
                primary = ViewModel.Settings.PrimaryColor;
                break;
            case 1: // 壁纸主题色
            case 3: // 屏幕主题色
                primary = ViewModel.Settings.SelectedPlatte;
                break;
            case 2:
                try
                {
                    primary = null;
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

        ControlColorHelper.SetControlForegroundColor(ResourceLoaderBorder, ViewModel.Settings.CustomForegroundColor,
            ViewModel.Settings.IsCustomForegroundColorEnabled);

        App._isCriticalSafeModeEnabled = ViewModel.Settings.IsCriticalSafeMode;
        SizeToContent = SizeToContent.Height;
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

        // BUG: 这个不生效！！！！！！
        PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.SkipManagement,
            ViewModel.Settings.WindowLayer == 1 || ViewModel.IsNotificationWindowExplicitShowed);
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

    private void MainWindow_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!ViewModel.IsClosing && (e.CloseReason != WindowCloseReason.OSShutdown &&
                                     e.CloseReason != WindowCloseReason.ApplicationShutdown))
        {
            e.Cancel = true;
            return;
        }
        AppBase.Current.Stop();
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
        // 和 WPF 不同，Avalonia 定位窗口用的基于物理屏幕的像素坐标，而非逻辑坐标，无需 dpi 转换。
        var x = screen.WorkingArea.X + ox;
        var y = ViewModel.Settings.WindowDockingLocation switch
        {
            0 => //左上
                //Left = (screen.WorkingArea.Left + ox) / dpiX;
                (offsetAreaTop + oy),
            1 => // 中上
                //Left = (c - aw / 2 + ox) / dpiX;
                (offsetAreaTop + oy),
            2 => // 右上
                //Left = (screen.WorkingArea.Right - aw + ox) / dpiX;
                (offsetAreaTop + oy),
            3 => // 左下
                //Left = (screen.WorkingArea.Left + ox) / dpiX;
                (offsetAreaBottom - ah + oy),
            4 => // 中下
                //Left = (c - aw / 2 + ox) / dpiX;
                (offsetAreaBottom - ah + oy),
            5 => // 右下
                //Left = (screen.WorkingArea.Right - aw + ox) / dpiX;
                (offsetAreaBottom - ah + oy),
            _ => 0.0
        };
        var newPos = new PixelPoint((int)x, (int)y);
        if (Position != newPos)
        {
            PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.SkipManagement, false);
            Position = newPos;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // PlatformServices.WindowPlatformService.ClearWindow(this);
                PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.SkipManagement,
                    ViewModel.Settings.WindowLayer == 1 || ViewModel.IsNotificationWindowExplicitShowed);
            }, DispatcherPriority.ApplicationIdle);
        }

        if (updateEffectWindow)
        {
            TopmostEffectWindow.UpdateWindowPos(screen, 1 / dpiX);
        }
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
            var screen = GetSelectedScreenSafe() ?? Screens.ScreenFromWindow(this);
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
        App.GetService<ProfileSettingsWindow>().OpenDrawer("TemporaryClassPlan");
        OpenProfileSettingsWindow();
    }

    public void OpenProfileSettingsWindow()
    {
        App.GetService<ProfileSettingsWindow>().Open();
    }

    private void MenuItemAbout_OnClick(object sender, EventArgs e)
    {
        App.GetService<SettingsWindowNew>().Open("about");
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
            App.GetService<ProfileSettingsWindow>().OpenDrawer("TemporaryClassPlan");
            OpenProfileSettingsWindow();
            return;
        }
        
        ViewModel.IsBusy = true;
        var ccw = new ClassChangingWindow()
        {
            ClassPlan = LessonsService.CurrentClassPlan
        };
        await ccw.ShowDialog(this);
        ViewModel.IsBusy = false;
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

        ProfileService.Profile.TempClassPlanId = Guid.TryParse(input.Text, out var guid) ? guid : Guid.Empty;
        ProfileService.Profile.TempClassPlanSetupTime = ExactTimeService.GetCurrentLocalDateTime();
    }

    private void NativeMenuItemDebugCrashTest_OnClick(object? sender, EventArgs e)
    {
        var window = new CrashWindow();
        window.Show();
    }

    private void NativeMenuItemDebugDevPortal_OnClick(object? sender, EventArgs e)
    {
        IAppHost.GetService<DevPortalWindow>().Show();
    }

    public void AcquireTopmostLock(object o)
    {
        var prevEmpty = TopmostLocks.Count <= 0;
        TopmostLocks.Add(o);
        if (!prevEmpty)
        {
            return;
        }

        ViewModel.IsNotificationWindowExplicitShowed = true;
        if (ViewModel.IsNotificationWindowExplicitShowed && SettingsService.Settings.WindowLayer == 0)
        {
            UpdateWindowLayer();
            ReCheckTopmostState();
        }
    }

    public void ReleaseTopmostLock(object o)
    {
        TopmostLocks.Remove(o);

        if (TopmostLocks.Count > 0)
        {
            return;
        }
        
        if (ViewModel.IsNotificationWindowExplicitShowed)
        {
            ViewModel.IsNotificationWindowExplicitShowed = false;
            SetBottom();
            UpdateWindowLayer();
        }
    }

    private void NativeMenuItemDebugOpenWelcomeWindow_OnClick(object? sender, EventArgs e)
    {
        IAppHost.GetService<WelcomeWindow>().Show();
    }

    private void NativeMenuItemDebugOpenWelcomeWindowFull_OnClick(object? sender, EventArgs e)
    {
        ViewModel.Settings.IsWelcomeWindowShowed = false;
        AppBase.Current.Restart();
    }

    private void LayoutContainerGrid_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() => Height = LayoutContainerGrid.Bounds.Height, DispatcherPriority.Render);
    }
}
