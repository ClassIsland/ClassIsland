using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.Threading;
using ClassIsland.Controls.NotificationEffects;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Models.Components;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Models.EventArgs;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Views;
using Linearstar.Windows.RawInput;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Org.BouncyCastle.Bcpg.Sig;
using ReactiveUI;
using Animation = Avalonia.Animation.Animation;
using Cue = Avalonia.Animation.Cue;
using NotificationRequest = ClassIsland.Core.Models.Notification.NotificationRequest;

namespace ClassIsland.Controls;

// [ContentProperty("Content")]
[TemplatePart(Name = "PART_GridWrapper", Type = typeof(Grid))]
[PseudoClasses(":dock-left", ":dock-right", ":dock-center", ":dock-top", ":dock-bottom",
    ":faded", ":mask-anim", ":overlay-anim", ":mask-in", ":overlay-in", ":mask-out", ":overlay-out", ":custom-background")]
public class MainWindowLine : TemplatedControl, INotificationConsumer
{
    public static readonly StyledProperty<object> ContentProperty = AvaloniaProperty.Register<MainWindowLine, object>(
        nameof(Content));
    
    [Content]
    public object Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    
}

    public static readonly StyledProperty<string> LastStoryboardNameProperty = AvaloniaProperty.Register<MainWindowLine, string>(
        nameof(LastStoryboardName));
    public string LastStoryboardName
    {
        get => GetValue(LastStoryboardNameProperty);
        set => SetValue(LastStoryboardNameProperty, value);
    
}

    public static readonly StyledProperty<bool> IsOverlayOpenProperty = AvaloniaProperty.Register<MainWindowLine, bool>(
        nameof(IsOverlayOpen));
    public bool IsOverlayOpen
    {
        get => GetValue(IsOverlayOpenProperty);
        set => SetValue(IsOverlayOpenProperty, value);
    
}
    
    public static readonly StyledProperty<double> BackgroundWidthProperty = AvaloniaProperty.Register<MainWindowLine, double>(
        nameof(BackgroundWidth));

    public double BackgroundWidth
    {
        get => GetValue(BackgroundWidthProperty);
        set => SetValue(BackgroundWidthProperty, value);
    }

    public static readonly StyledProperty<int> WindowDockingLocationProperty = AvaloniaProperty.Register<MainWindowLine, int>(
        nameof(WindowDockingLocation));

    public int WindowDockingLocation
    {
        get => GetValue(WindowDockingLocationProperty);
        set => SetValue(WindowDockingLocationProperty, value);
    }
    
    public static readonly StyledProperty<bool> IsMainLineProperty = AvaloniaProperty.Register<MainWindowLine, bool>(
        nameof(IsMainLine));
    public bool IsMainLine
    {
        get => GetValue(IsMainLineProperty);
        set => SetValue(IsMainLineProperty, value);
    
    }

    public static readonly StyledProperty<bool> IsNotificationEnabledProperty = AvaloniaProperty.Register<MainWindowLine, bool>(
        nameof(IsNotificationEnabled));

    public bool IsNotificationEnabled
    {
        get => GetValue(IsNotificationEnabledProperty);
        set => SetValue(IsNotificationEnabledProperty, value);
    }
    
    public static readonly StyledProperty<bool> IsMouseInProperty = AvaloniaProperty.Register<MainWindowLine, bool>(
        nameof(IsMouseIn));

    public bool IsMouseIn
    {
        get => GetValue(IsMouseInProperty);
        set => SetValue(IsMouseInProperty, value);
    }

    public static readonly StyledProperty<int> LineNumberProperty = AvaloniaProperty.Register<MainWindowLine, int>(
        nameof(LineNumber));
    public int LineNumber
    {
        get => GetValue(LineNumberProperty);
        set => SetValue(LineNumberProperty, value);
    
}

    public static readonly StyledProperty<bool> IsAllComponentsHidProperty = AvaloniaProperty.Register<MainWindowLine, bool>(
        nameof(IsAllComponentsHid));
    public bool IsAllComponentsHid
    {
        get => GetValue(IsAllComponentsHidProperty);
        set => SetValue(IsAllComponentsHidProperty, value);
    
}

    public static readonly StyledProperty<bool> IsLineFadedProperty = AvaloniaProperty.Register<MainWindowLine, bool>(
        nameof(IsLineFaded));
    public bool IsLineFaded
    {
        get => GetValue(IsLineFadedProperty);
        set => SetValue(IsLineFadedProperty, value);
    
}

    public static readonly StyledProperty<MainWindowLineSettings> SettingsProperty = AvaloniaProperty.Register<MainWindowLine, MainWindowLineSettings>(
        nameof(Settings));

    public MainWindowLineSettings Settings
    {
        get => GetValue(SettingsProperty);
        set => SetValue(SettingsProperty, value);
    }

    private NotificationContent? _maskContent;

    public static readonly DirectProperty<MainWindowLine, NotificationContent?> MaskContentProperty = AvaloniaProperty.RegisterDirect<MainWindowLine, NotificationContent?>(
        nameof(MaskContent), o => o.MaskContent, (o, v) => o.MaskContent = v);

    public NotificationContent? MaskContent
    {
        get => _maskContent;
        set => SetAndRaise(MaskContentProperty, ref _maskContent, value);
    }

    private NotificationContent? _overlayContent;

    public static readonly DirectProperty<MainWindowLine, NotificationContent?> OverlayContentProperty = AvaloniaProperty.RegisterDirect<MainWindowLine, NotificationContent?>(
        nameof(OverlayContent), o => o.OverlayContent, (o, v) => o.OverlayContent = v);

    public NotificationContent? OverlayContent
    {
        get => _overlayContent;
        set => SetAndRaise(OverlayContentProperty, ref _overlayContent, value);
    }

    public static readonly StyledProperty<double> CountdownProgressValueProperty = AvaloniaProperty.Register<MainWindowLine, double>(
        nameof(CountdownProgressValue));

    public double CountdownProgressValue
    {
        get => GetValue(CountdownProgressValueProperty);
        set => SetValue(CountdownProgressValueProperty, value);
    }

    private bool _isOverlayOpen = false;
    
    private DateTime _firstProcessNotificationsTime = DateTime.MinValue;

    private INotificationHostService NotificationHostService { get; } = IAppHost.GetService<INotificationHostService>();

    private Queue<NotificationRequest> _notificationQueue = new();

    private bool _isLoadCompleted = false;

    private bool _isTemplateApplied = false;

    public MainWindow MainWindow { get; } = IAppHost.GetService<MainWindow>();

    public SettingsService SettingsService { get; } = IAppHost.GetService<SettingsService>();

    private DispatcherTimer TouchInFadingTimer { get; set; } = new();

    private ILogger<MainWindowLine> Logger { get; } = IAppHost.GetService<ILogger<MainWindowLine>>();

    private IComponentsService ComponentsService { get; } = IAppHost.GetService<IComponentsService>();

    private IExactTimeService ExactTimeService { get; } = IAppHost.GetService<IExactTimeService>();

    private ISpeechService SpeechService { get; } = IAppHost.GetService<ISpeechService>();

    private TopmostEffectWindow TopmostEffectWindow { get; } = IAppHost.GetService<TopmostEffectWindow>();

    private Grid? GridWrapper;
    
    private Point _centerPointCache = new Point(0, 0);

    private object TopmostLock { get; } = new();

    public static FuncValueConverter<double, Thickness> DoubleToThicknessTopConverter { get; } =
        new(x => new Thickness(0, x, 0, 0));
    
    public static FuncValueConverter<double, Thickness> DoubleToThicknessBottomConverter { get; } =
        new(x => new Thickness(0, 0, 0, x));

    public MainWindowLine()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ComponentPresenter.ComponentVisibilityChangedEvent.AddClassHandler(typeof(MainWindowLine), 
            UpdateVisibilityState, RoutingStrategies.Tunnel);

        this.GetObservable(WindowDockingLocationProperty)
            .Skip(1)
            .Subscribe(_ => UpdateStyleStates());
        this.GetObservable(IsMouseInProperty)
            .Skip(1)
            .Subscribe(_ => UpdateFadeStatus());
        SettingsService.Settings.ObservableForProperty(x => x.IsCustomBackgroundColorEnabled)
            .Subscribe(v => PseudoClasses.Set(":custom-background", v.Value));
    }

    private void UpdateStyleStates()
    {
        PseudoClasses.Set(":dock-left", WindowDockingLocation is 0 or 3);
        PseudoClasses.Set(":dock-center", WindowDockingLocation is 1 or 4);
        PseudoClasses.Set(":dock-right", WindowDockingLocation is 2 or 5);
        PseudoClasses.Set(":dock-top", WindowDockingLocation is 0 or 1 or 2);
        PseudoClasses.Set(":dock-bottom", WindowDockingLocation is 3 or 4 or 5);
    }

    private void UpdateVisibilityState(object? sender, RoutedEventArgs args)
    {
        Logger.LogTrace("ComponentVisibilityChangedEvent handled");
        IsAllComponentsHid = Settings.Children
            .Where(x => x.RelativeLineNumber == LineNumber)
            .FirstOrDefault(x => x.IsVisible) == null;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        MainWindow.MousePosChanged += MainWindowOnMousePosChanged;
        MainWindow.RawInputEvent += MainWindowOnRawInputEvent;
        MainWindow.MainWindowAnimationEvent += MainWindowOnMainWindowAnimationEvent;
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        UpdateFadeStatus();
        NotificationHostService.RegisterNotificationConsumer(this, Settings.IsMainLine ? -1 : LineNumber);

        _isLoadCompleted = true;
    }


    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        MainWindow.MousePosChanged -= MainWindowOnMousePosChanged;
        MainWindow.RawInputEvent -= MainWindowOnRawInputEvent;
        MainWindow.MainWindowAnimationEvent -= MainWindowOnMainWindowAnimationEvent;
        SettingsService.Settings.PropertyChanged -= SettingsOnPropertyChanged;
        NotificationHostService.UnregisterNotificationConsumer(this);
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsService.Settings.IsMouseInFadingReversed)
            or nameof(SettingsService.Settings.IsMouseInFadingEnabled))
        {
            UpdateFadeStatus();
        }
    }

    private void UpdateFadeStatus()
    {
        IsLineFaded =
            SettingsService.Settings.IsMouseInFadingEnabled &&
            (IsMouseIn ^ SettingsService.Settings.IsMouseInFadingReversed);
    }

    private void MainWindowOnMainWindowAnimationEvent(object? sender, MainWindowAnimationEventArgs e)
    {
        if (!IsMainLine)
        {
            return;
        }
        // BeginStoryboard(e.StoryboardName);
    }

    private void MainWindowOnRawInputEvent(object? sender, RawInputEventArgs e)
    {
        switch (e.Data)
        {
            case RawInputDigitizerData digitizerData:
            {
                var contacts = digitizerData.Contacts;
                //Logger.LogTrace("TOUCH {}", string.Join(", ", contacts.ToList().Select(x => $"({x.X}, {x.Y} + {x.Width})")));
                var r = IsMouseIn =
                    contacts.ToList().Exists(x => GetMouseStatusByPos(new Point(x.X, x.Y)));
                if (SettingsService.Settings.TouchInFadingDurationMs > 0 && r)
                {
                    TouchInFadingTimer.Stop();
                    TouchInFadingTimer.Interval = TimeSpan.FromMilliseconds(SettingsService.Settings.TouchInFadingDurationMs);
                    TouchInFadingTimer.Start();
                }

                if (!r)
                {
                    TouchInFadingTimer.Stop();
                }
                break;
            }
            case RawInputMouseData mouseData:
                //Logger.LogTrace("MOUSE ({}, {}) {}", mouseData.Mouse.LastX, mouseData.Mouse.LastY, mouseData.Mouse.Buttons);
                //if (TouchInFadingTimer.IsEnabled)
                TouchInFadingTimer.Stop();
                UpdateMouseStatus();
                break;
        }
    }

    private void UpdateMouseStatus()
    {
        // if (PresentationSource.FromVisual(this) == null)
        // {
        //     return;
        // }

        try
        {
            var ptr = PlatformServices.WindowPlatformService.GetMousePos();
            IsMouseIn = GetMouseStatusByPos(ptr);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "无法更新鼠标状态。");
        }
    }

    private void MainWindowOnMousePosChanged(object? sender, MousePosChangedEventArgs e)
    {
        IsMouseIn = GetMouseStatusByPos(e.Pos);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        Logger.LogTrace("已应用控件模板");
        if (this.GetTemplateChildren().OfType<Grid>().FirstOrDefault(x => x.Name == "PART_GridWrapper") is { } wrapper)
        {
            if (GridWrapper is not null)
            {
                GridWrapper.SizeChanged -= WrapperOnSizeChanged;
            }
            GridWrapper = wrapper;
            wrapper.SizeChanged += WrapperOnSizeChanged;
            wrapper.Loaded += GridWrapperOnLoaded;
        }
        base.OnApplyTemplate(e);
    }

    private void GridWrapperOnLoaded(object? sender, RoutedEventArgs e)
    {
        if (GridWrapper != null)
        {
            GridWrapper.Loaded -= GridWrapperOnLoaded;
            GridWrapper.Unloaded -= GridWrapperOnUnloaded;
            GridWrapper.Unloaded += GridWrapperOnUnloaded;
        }
    }

    private void GridWrapperOnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (GridWrapper != null)
        {
            GridWrapper.Unloaded -= GridWrapperOnUnloaded;
            GridWrapper.Loaded -= GridWrapperOnLoaded;
            GridWrapper.Loaded += GridWrapperOnLoaded;
        }

        Logger.LogTrace("GridWrapper Unloaded");
        _isTemplateApplied = false;
    }

    private async void WrapperOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        BackgroundWidth = e.NewSize.Width;
    }

    private bool GetMouseStatusByPos(Point ptr)
    {
        if (GridWrapper == null)
        {
            return false;
        }
        MainWindow.GetCurrentDpi(out var dpiX, out var dpiY);
        var scale = SettingsService.Settings.Scale;
        //Debug.WriteLine($"Window: {Left * dpiX} {Top * dpiY};; Cursor: {ptr.X} {ptr.Y} ;; dpi: {dpiX}");
        var root = GridWrapper.PointToScreen(new Point(0, 0));
        var cx = root.X;
        var cy = root.Y;
        var cw = GridWrapper.Bounds.Width * dpiX * scale;
        var ch = GridWrapper.Bounds.Height * dpiY * scale;
        var cr = cx + cw;
        var cb = cy + ch;

        return (cx <= ptr.X && cy <= ptr.Y && ptr.X <= cr && ptr.Y <= cb);
    }

    public void ReceiveNotifications(IReadOnlyList<NotificationRequest> notificationRequests)
    {
        foreach (var newRequest in notificationRequests)
        {
            _notificationQueue.Enqueue(newRequest);
        }

        ProcessNotification();
    }
    
    private void PreProcessNotificationContent(NotificationContent content)
    {
        if (content.EndTime != null)  // 如果目标结束时间为空，那么就计算持续时间
        {
            var rawTime = content.EndTime.Value - ExactTimeService.GetCurrentLocalDateTime();
            content.Duration = rawTime > TimeSpan.Zero ? rawTime : TimeSpan.Zero;
        }

        if (content.ContentTemplateResourceKey != null && 
            this.TryFindResource(content.ContentTemplateResourceKey, out var template))
        {
            content.ContentTemplate = template as DataTemplate;
        }
    }
    
    private Point GetCenter()
    {
        var scale = SettingsService.Settings.Scale;
        // 在切换组件配置时可能出现找不到 GridWrapper 的情况，此时要使用上一次的数值
        var p = GridWrapper?.TranslatePoint(new Point(GridWrapper.Bounds.Width / 2, GridWrapper.Bounds.Height / 2), this);
        if (p == null)
        {
            return _centerPointCache;
        }
        return _centerPointCache = new Point(p.Value.X * scale, (Bounds.Top + (Bounds.Height / 2)) * scale);
    }

    private async void ProcessNotification()
    {
        if (_isOverlayOpen)
        {
            return;
        }
        _isOverlayOpen = true;  // 上锁

        var notificationsShowed = false;

        if (_firstProcessNotificationsTime == DateTime.MinValue)
            _firstProcessNotificationsTime = ExactTimeService.GetCurrentLocalDateTime();
        if (!SettingsService.Settings.IsNotificationEnabled ||
            (ExactTimeService.GetCurrentLocalDateTime() - _firstProcessNotificationsTime <= TimeSpan.FromSeconds(10) &&
             App.ApplicationCommand.Quiet) // 静默启动
           )
        {
            NotificationHostService.RequestQueue.Clear();
        }

        while (_notificationQueue.Count > 0)
        {
            using var player = new DirectSoundOut();
            var request = _notificationQueue.Dequeue();
            INotificationSettings settings = SettingsService.Settings;
            foreach (var i in new List<NotificationSettings?>([request.ChannelSettings, request.ProviderSettings, request.RequestNotificationSettings]).OfType<NotificationSettings>().Where(i => i.IsSettingsEnabled))
            {
                settings = i;
                break;
            }
            var mask = request.MaskContent;
            var overlay = request.OverlayContent;
            var isMaskSpeechEnabled = settings.IsSpeechEnabled && request.MaskContent.IsSpeechEnabled && SettingsService.Settings.AllowNotificationSpeech;
            var isOverlaySpeechEnabled = request.OverlayContent != null && settings.IsSpeechEnabled && request.OverlayContent.IsSpeechEnabled && SettingsService.Settings.AllowNotificationSpeech;
            Logger.LogInformation("处理通知请求：{} {}", request.MaskContent.GetType(), request.OverlayContent?.GetType());
            var cancellationToken = request.CancellationTokenSource.Token;

            PreProcessNotificationContent(mask);


            if (request.MaskContent.Duration > TimeSpan.Zero && !cancellationToken.IsCancellationRequested)
            {
                PseudoClasses.Set(":mask-anim", false);
                PseudoClasses.Set(":overlay-out", false);
                PseudoClasses.Set(":mask-in", false);
                PseudoClasses.Set(":mask-out", false);
                notificationsShowed = true;
                MaskContent = request.MaskContent;  // 加载Mask元素
                if (settings.IsNotificationTopmostEnabled && SettingsService.Settings.AllowNotificationTopmost)
                {
                    MainWindow.AcquireTopmostLock(TopmostLock);
                }
                else
                {
                    MainWindow.ReleaseTopmostLock(TopmostLock);
                }

                if (isMaskSpeechEnabled)
                {
                    SpeechService.EnqueueSpeechQueue(request.MaskContent.SpeechContent);
                }
                PseudoClasses.Set(":mask-anim", true);
                PseudoClasses.Set(":mask-in", true);
                PseudoClasses.Set(":overlay-anim", false);
                // 播放提醒音效
                if (settings.IsNotificationSoundEnabled && SettingsService.Settings.AllowNotificationSound)
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
                if (settings.IsNotificationEffectEnabled && SettingsService.Settings.AllowNotificationEffect &&
                    !IsAllComponentsHid && SettingsService.Settings.IsMainWindowVisible)
                {
                    var center = GetCenter();
                    TopmostEffectWindow.PlayEffect(new RippleEffect()
                    {
                        CenterX = center.X,
                        CenterY = center.Y
                    });
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Run(() => cancellationToken.WaitHandle.WaitOne(request.MaskContent.Duration), cancellationToken);
                }
                if (overlay is null || cancellationToken.IsCancellationRequested || overlay.Duration <= TimeSpan.Zero)
                {
                    PseudoClasses.Set(":overlay-anim", true);
                    PseudoClasses.Set(":mask-in", false);
                    PseudoClasses.Set(":mask-out", true);
                    PseudoClasses.Set(":overlay-in", false);
                    PseudoClasses.Set(":overlay-out", true);
                }
                else
                {
                    PreProcessNotificationContent(overlay);
                    OverlayContent = overlay;
                    if (isOverlaySpeechEnabled)
                    {
                        SpeechService.EnqueueSpeechQueue(overlay.SpeechContent);
                    }
                    PseudoClasses.Set(":mask-out", true);
                    PseudoClasses.Set(":mask-in", false);
                    PseudoClasses.Set(":overlay-out", false);
                    PseudoClasses.Set(":overlay-in", true);
                    var animation = new Animation()
                    {
                        Duration = overlay.Duration,
                        Children =
                        {
                            new KeyFrame()
                            {
                                Cue = new Cue(0.0),
                                Setters =
                                {
                                    new Setter(CountdownProgressValueProperty, 1.0)
                                }
                            },
                            new KeyFrame()
                            {
                                Cue = new Cue(1.0),
                                Setters =
                                {
                                    new Setter(CountdownProgressValueProperty, 0.0)
                                }
                            }
                        }
                    };
                    _ = animation.RunAsync(this, cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Run(() => cancellationToken.WaitHandle.WaitOne(overlay.Duration),
                            cancellationToken);
                    }
                }
                SpeechService.ClearSpeechQueue();
            }

            if (NotificationHostService.RequestQueue.Count < 1 && notificationsShowed)
            {
                PseudoClasses.Set(":overlay-anim", true);
                PseudoClasses.Set(":overlay-out", true);
                PseudoClasses.Set(":overlay-in", false);
            }
            await request.CompletedTokenSource.CancelAsync();

            var notifications = NotificationHostService.PullNotificationRequests();
            foreach (var newRequest in notifications)
            {
                _notificationQueue.Enqueue(newRequest);
            }
        }

        OverlayContent = null;
        MaskContent = null;
        _isOverlayOpen = false;
        MainWindow.ReleaseTopmostLock(TopmostLock);
    }

    public int QueuedNotificationCount => _notificationQueue.Count;
    public bool AcceptsNotificationRequests => IsNotificationEnabled && !IsAllComponentsHid && !_isOverlayOpen;
}