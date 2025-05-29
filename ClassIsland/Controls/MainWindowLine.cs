using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data.Core;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Models.Components;
using ClassIsland.Models.EventArgs;
using ClassIsland.Services;
using ClassIsland.Shared;
using Linearstar.Windows.RawInput;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Controls;

// [ContentProperty("Content")]
[TemplatePart(Name = "PART_GridWrapper", Type = typeof(Grid))]
[PseudoClasses(":dock-left", ":dock-right", ":dock-center", ":dock-top", ":dock-bottom",
    "faded", "mask-open", "overlay-open", "custom-background")]
public class MainWindowLine : TemplatedControl
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

    private bool _isLoadCompleted = false;

    private bool _isTemplateApplied = false;

    public MainWindow MainWindow { get; } = IAppHost.GetService<MainWindow>();

    public SettingsService SettingsService { get; } = IAppHost.GetService<SettingsService>();

    private DispatcherTimer TouchInFadingTimer { get; set; } = new();

    private ILogger<MainWindowLine> Logger { get; } = IAppHost.GetService<ILogger<MainWindowLine>>();

    private IComponentsService ComponentsService { get; } = IAppHost.GetService<IComponentsService>();

    private Grid? GridWrapper;

    public MainWindowLine()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ComponentPresenter.ComponentVisibilityChangedEvent.AddClassHandler(typeof(MainWindowLine), 
            UpdateVisibilityState, RoutingStrategies.Tunnel);

        this.GetObservable(WindowDockingLocationProperty)
            .Skip(1)
            .Subscribe(_ => UpdateStyleStates());
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

        _isLoadCompleted = true;
    }


    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        MainWindow.MousePosChanged -= MainWindowOnMousePosChanged;
        MainWindow.RawInputEvent -= MainWindowOnRawInputEvent;
        MainWindow.MainWindowAnimationEvent -= MainWindowOnMainWindowAnimationEvent;
        SettingsService.Settings.PropertyChanged -= SettingsOnPropertyChanged;
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
                    contacts.ToList().Exists(x => GetMouseStatusByPos(new System.Drawing.Point(x.X, x.Y)));
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
            GetCursorPos(out var ptr);
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

        Logger.LogDebug("LastStoryboardName = {}", LastStoryboardName);
        if (IsMainLine && LastStoryboardName != null && IsOverlayOpen)
        {
            // BeginStoryboard(LastStoryboardName);
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


        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _isTemplateApplied = true;
            Logger.LogDebug("LastStoryboardName = {}", LastStoryboardName);
            if (IsMainLine && LastStoryboardName != null && IsOverlayOpen)
            {
                // BeginStoryboard(LastStoryboardName);
            }
        }, DispatcherPriority.Loaded);
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

    private void WrapperOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (double.IsNaN(e.NewSize.Width))
            return;
        if (double.IsNaN(BackgroundWidth))
        {
            BackgroundWidth = e.NewSize.Width;
            return;
        }

        var m = e.NewSize.Width > BackgroundWidth;
        var s = 1.0;  // TODO: 适配 debug 动画缩放
        var t = m ? 600 * s : 800 * s;
        var anim = new Animation()
        {
            Duration = TimeSpan.FromMilliseconds(t),
            Easing = new BackEaseOut(),
            Children =
            {
                new KeyFrame()
                {
                    Setters =
                    {
                        new Setter(BackgroundWidthProperty, BackgroundWidth)
                    },
                    Cue = new Cue(0.0)
                },
                new KeyFrame()
                {
                    Setters =
                    {
                        new Setter(BackgroundWidthProperty, e.NewSize.Width)
                    },
                    Cue = new Cue(1.0)
                }
            }
        };
        anim.RunAsync(this);
    }

    private bool GetMouseStatusByPos(System.Drawing.Point ptr)
    {
        if (GridWrapper == null)
        {
            return false;
        }
        MainWindow.GetCurrentDpi(out var dpiX, out var dpiY);
        var scale = MainWindow.ViewModel.Settings.Scale;
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
}