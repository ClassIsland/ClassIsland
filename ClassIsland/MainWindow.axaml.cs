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
using ClassIsland.Controls.EditMode;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Assists;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Components;
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
using ClassIsland.Shared.Interfaces.Controls;
using ClassIsland.ViewModels;
using ClassIsland.Views;
using CommunityToolkit.Mvvm.Input;
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
[PseudoClasses(":dock-top", ":dock-bottom", ":edit-mode", ":windowed")]
public partial class MainWindow : Window, ITopmostEffectPlayer
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

    private ClassChangingWindow? ClassChangingWindow { get; set; }
    
    private IUriNavigationService UriNavigationService { get; }
    public IRulesetService RulesetService { get; }
    public IWindowRuleService WindowRuleService { get; }
    public IManagementService ManagementService { get; }

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

    public NativeMenu MoreOptionsMenu { get; } = [];

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
        TaskBarIconService.MoreOptionsMenu = MoreOptionsMenu;
        
        PointerStateAssist.SetIsTouchMode(this, true);  // DEBUG
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
        if (!OperatingSystem.IsMacOS())
        {
            PopupHelper.DisablePopupsRequested += (_, _) =>
            {
                TaskBarIconService.MainTaskBarIcon.Menu = null;
            };
            PopupHelper.RestorePopupsRequested += (_, _) =>
            {
                TaskBarIconService.MainTaskBarIcon.Menu = menu;
            };
        }
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
        IAppHost.GetService<ISplashService>().SetDetailedStatus("正在加载界面主题（2）");
        UpdateTheme();
        base.Show();
        UpdateWindowPos();
        Win32Properties.AddWndProcHookCallback(this, ProcWnd);
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
        if (ViewModel.IsWindowMode)
        {
            return;
        }
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

        if (e.PropertyName == nameof(ViewModel.IsEditMode))
        {
            PseudoClasses.Set(":edit-mode", ViewModel.IsEditMode);
            if (ViewModel.IsEditMode)
            {
                Activate();
                EditModeViewCp.Content = ViewModel.EditModeView = IAppHost.GetService<EditModeView>();
            }
            else
            {
                EditModeViewCp.Content = ViewModel.EditModeView = null;
                ZoomBorder.ResetMatrix();
            }
            UpdateWindowLayer();
            UpdateTheme();
            _ = Dispatcher.UIThread.InvokeAsync(UpdateTheme);
        }
        
        if (e.PropertyName == nameof(ViewModel.IsWindowMode))
        {
            PseudoClasses.Set(":windowed", ViewModel.IsWindowMode);
            UpdateTheme();
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
        if (ViewModel.IsNotificationWindowExplicitShowed || ViewModel.IsEditMode)
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
        UpdateWindowFeatures();
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

    private void UpdateWindowFeatures()
    {
        PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.ToolWindow, !ViewModel.IsWindowMode);
        if (!ViewModel.Settings.IsMouseClickingEnabled && !ViewModel.IsEditMode)
        {
            PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Transparent, true);
        }
        else
        {
            PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Transparent, false);
        }
    }

    private void UpdateWindowLayer()
    {
        if (ViewModel.IsWindowMode)
        {
            Topmost = false;
            PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Topmost, false);
            PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Bottommost, false);
            PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.SkipManagement, false);
            return;
        }
        switch (ViewModel.Settings.WindowLayer)
        {
            case 0: // bottom
                Topmost = ViewModel.IsNotificationWindowExplicitShowed || ViewModel.IsEditMode;
                break;
            case 1:
                Topmost = true;
                break;
        }

        PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.SkipManagement, Topmost);
        PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Topmost, Topmost);
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
            if (ViewModel.IsEditMode)
            {
                ViewModel.IsEditMode = false;
            }
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

        if (ViewModel.IsWindowMode)
        {
            ViewModel.ActualClientBound = new Rect(0, 0, Width, Height);
            LayoutContainerGrid.Width = Width;
            LayoutContainerGrid.Height = Height;
            return;
        }

        WindowState = WindowState.Normal;
        var screen = GetSelectedScreenSafe();
        if (screen == null)
            return;
        var clientRect = ViewModel.Settings.IsIgnoreWorkAreaEnabled ? screen.Bounds : screen.WorkingArea;
        var c = (double)(screen.WorkingArea.X + screen.WorkingArea.Right) / 2;
        var ox = ViewModel.Settings.WindowDockingOffsetX / dpiX;
        var oy = ViewModel.Settings.WindowDockingOffsetY / dpiY;
        var fullscreen = ViewModel.IsForegroundFullscreen;
        var bounds = fullscreen ? screen.Bounds : clientRect;
        var relativeX = clientRect.X - screen.Bounds.X;
        var relativeY = clientRect.Y - screen.Bounds.Y;
        var width = bounds.Width;
        var height = bounds.Height;
        // 创建相对矩形
        var clientBoundsRelative = new PixelRect(relativeX, relativeY, width, height)
            .ToRectWithDpi(new Vector(dpiX * 96, dpiY * 96));
        ViewModel.ActualClientBound = clientBoundsRelative;
        LayoutContainerGrid.Width = Width = screen.Bounds.Width / dpiX;
        LayoutContainerGrid.Height = Height = (screen.Bounds.Height - 1)  // 防止 Windows 发电误以为是全屏
                                              / dpiY;
        ViewModel.ActualRootOffsetX = ox;
        ViewModel.ActualRootOffsetY = oy;
        var newPos = new PixelPoint((int)screen.Bounds.X, (int)screen.Bounds.Y);
        if (Position != newPos)
        {
            Position = newPos;
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
        // 直接监听窗口属性变化并设置鼠标穿透可能导致死循环/栈溢出，故我们在用户点到 ClassIsland 窗口时设置这个。
        UpdateWindowFeatures();
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

        if (ClassChangingWindow != null)
        {
            return;
        }
        
        // ViewModel.IsBusy = true;
        ClassChangingWindow = new ClassChangingWindow()
        {
            ClassPlan = LessonsService.CurrentClassPlan
        };
        await ClassChangingWindow.ShowDialog(this);
        ClassChangingWindow.DataContext = null;
        ClassChangingWindow = null;
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
        if (TopmostLocks.Contains(o))
        {
            return;
        }
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
        TopmostLocks.RemoveAll(x => x == o);

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
        // Dispatcher.UIThread.InvokeAsync(() => Height = LayoutContainerGrid.Bounds.Height, DispatcherPriority.Render);
    }

    private void NativeMenuItemDebugOpenScreenshotWindow_OnClick(object? sender, EventArgs e)
    {
        IAppHost.GetService<ScreenshotHelperWindow>().Show();
    }

    public void PlayEffect(INotificationEffectControl effect)
    {
        Logger.LogInformation("播放顶层特效：{}", effect);
        if (effect is not Control element)
            return;
        ViewModel.EffectControls.Add(element);
        if (!element.IsLoaded)
        {
            element.Loaded += (sender, args) => SetupEffectVisual(element, effect);
        }
        else
        {
            SetupEffectVisual(element, effect);
        }
    }

    private void SetupEffectVisual(Control visual1, INotificationEffectControl effect)
    {
        effect.EffectCompleted += (sender, args) =>
        {
            Logger.LogInformation("结束播放并移除顶层特效：{}", effect);
            ViewModel.EffectControls.Remove(visual1);
        };
        effect.Play();
    }

    #region EditMode

    private async void NativeMenuItemEnterEditMode_OnClick(object? sender, EventArgs e)
    {
        if (!await ManagementService.AuthorizeByLevel(ManagementService.CredentialConfig.EditSettingsAuthorizeLevel))
        {
            return;
        }
        ViewModel.IsEditMode = true;
    }

    private void ButtonExitEditMode_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.IsEditMode = false;
        ComponentsService.SaveConfig();
    }

    private void ButtonOpenComponentsLib_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.EditModeView?.OpenComponentsLibDrawer();
    }

    private void ButtonOpenAppearanceSettings_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.EditModeView?.OpenAppearanceSettingsDrawer();
    }
    private void ListBoxMainWindowLineSettings_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not EditableComponentsListBox { Tag: MainWindowLineSettings settings } listBox)
        {
            return;
        }

        if (!ViewModel.ComponentsListBoxCache.Contains(listBox))
        {
            ViewModel.ComponentsListBoxCache.Add(listBox);
        }
        ViewModel.MainWindowLineListBoxCache[settings] = listBox;
        ViewModel.MainWindowLineListBoxCacheReversed[listBox] = settings;
    }

    private void ListBoxMainWindowLineSettings_OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not EditableComponentsListBox listBox)
        {
            return;
        }

        var settings = ViewModel.MainWindowLineListBoxCacheReversed.GetValueOrDefault(listBox);
        if (settings != null)
        {
            ViewModel.MainWindowLineListBoxCache.Remove(settings);
        }
        ViewModel.MainWindowLineListBoxCacheReversed.Remove(listBox);
        while (ViewModel.ComponentsListBoxCache.Contains(listBox))
        {
            ViewModel.ComponentsListBoxCache.Remove(listBox);
        }
    }
    
    private void ListBoxContainerComponent_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not EditableComponentsListBox { Tag: EditModeContainerComponentInfo settings } listBox)
        {
            return;
        }

        if (!ViewModel.ComponentsListBoxCache.Contains(listBox))
        {
            ViewModel.ComponentsListBoxCache.Add(listBox);
        }
        ViewModel.ContainerComponentListBoxCache[settings] = listBox;
        ViewModel.ContainerComponentListBoxCacheReversed[listBox] = settings;
    }

    private void ListBoxContainerComponent_OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not EditableComponentsListBox listBox)
        {
            return;
        }

        var settings = ViewModel.ContainerComponentListBoxCacheReversed.GetValueOrDefault(listBox);
        if (settings != null)
        {
            ViewModel.ContainerComponentListBoxCache.Remove(settings);
        }
        ViewModel.ContainerComponentListBoxCacheReversed.Remove(listBox);
        while (ViewModel.ComponentsListBoxCache.Contains(listBox))
        {
            ViewModel.ComponentsListBoxCache.Remove(listBox);
        }
    }
    
    private void SelectorComponents_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ViewModel.IsEditMode 
            || e.AddedItems.Count <= 0 || e.AddedItems[0] is not ComponentSettings settings 
            || sender is not EditableComponentsListBox { ItemsSource: IList<ComponentSettings> list } 
            || !list.Contains(settings))
        {
            // UpdateSettingsVisibility();
            return;
        }
        foreach (var listBox in ViewModel.ComponentsListBoxCache.Where(x => !Equals(x, sender)))
        {
            listBox.SelectedItem = null;
        }
        ViewModel.SelectedComponentSettings = settings;
        
    }

    [RelayCommand]
    public void ShowComponentSettings(ComponentSettings? component)
    {
        ViewModel.EditModeView?.ShowComponentSettings();
    }

    [RelayCommand]
    public void CloseContainerComponent(EditModeContainerComponentInfo? info)
    {
        ViewModel.EditModeView?.CloseContainerComponent(info);
    }
    
    private void EditableComponentsListBox_OnRequestOpenChildComponents(object? sender, EditableComponentsListBoxEventArgs e)
    {
        ViewModel.EditModeView?.OpenChildComponents(e.Settings, e.ComponentStack, GetContainerComponentEditContainerInitPos(e.ItemPosition));
    }

    public Point GetContainerComponentEditContainerInitPos(Point pos)
    {
        var transform = this.TransformToVisual(ContainerComponentsEditHost);
        var pointInCanvas = transform?.Transform(pos);
        if (pointInCanvas == null) 
            return new Point();
        var newPoint = new Point(pointInCanvas.Value.X, pointInCanvas.Value.Y + 48);
        return newPoint;
    }
    
    #endregion

}
