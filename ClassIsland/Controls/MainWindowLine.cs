using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml.Linq;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Models.EventArgs;
using ClassIsland.Services;
using ClassIsland.Shared;
using Linearstar.Windows.RawInput;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Controls;

[ContentProperty("Content")]
[TemplatePart(Name = "PART_GridWrapper", Type = typeof(Grid))]
public class MainWindowLine : Control
{
    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(MainWindowLine), new PropertyMetadata(default(object)));

    public object? Content
    {
        get { return (object)GetValue(ContentProperty); }
        set { SetValue(ContentProperty, value); }
    }

    public static readonly DependencyProperty BackgroundWidthProperty = DependencyProperty.Register(
        nameof(BackgroundWidth), typeof(double), typeof(MainWindowLine), new PropertyMetadata(default(double)));

    public static readonly DependencyProperty LastStoryboardNameProperty = DependencyProperty.Register(
        nameof(LastStoryboardName), typeof(string), typeof(MainWindowLine), new PropertyMetadata(default(string)));

    public string? LastStoryboardName
    {
        get { return (string)GetValue(LastStoryboardNameProperty); }
        set { SetValue(LastStoryboardNameProperty, value); }
    }

    public static readonly DependencyProperty IsOverlayOpenProperty = DependencyProperty.Register(
        nameof(IsOverlayOpen), typeof(bool), typeof(MainWindowLine), new PropertyMetadata(default(bool)));

    public bool IsOverlayOpen
    {
        get { return (bool)GetValue(IsOverlayOpenProperty); }
        set { SetValue(IsOverlayOpenProperty, value); }
    }

    public double BackgroundWidth
    {
        get { return (double)GetValue(BackgroundWidthProperty); }
        set { SetValue(BackgroundWidthProperty, value); }
    }

    public static readonly DependencyProperty WindowDockingLocationProperty = DependencyProperty.Register(
        nameof(WindowDockingLocation), typeof(int ), typeof(MainWindowLine), new PropertyMetadata(default(int )));


    public static readonly DependencyProperty IsMainLineProperty = DependencyProperty.Register(
        nameof(IsMainLine), typeof(bool), typeof(MainWindowLine), new PropertyMetadata(default(bool)));

    public bool IsMainLine
    {
        get { return (bool)GetValue(IsMainLineProperty); }
        set { SetValue(IsMainLineProperty, value); }
    }

    public int  WindowDockingLocation
    {
        get { return (int )GetValue(WindowDockingLocationProperty); }
        set { SetValue(WindowDockingLocationProperty, value); }
    }

    public static readonly DependencyProperty IsMouseInProperty = DependencyProperty.Register(
        nameof(IsMouseIn), typeof(bool), typeof(MainWindowLine), new PropertyMetadata(default(bool), (o, args) =>
        {
            if (o is MainWindowLine line)
            {
                line.UpdateFadeStatus();
            }
        }));

    public bool IsMouseIn
    {
        get { return (bool)GetValue(IsMouseInProperty); }
        set { SetValue(IsMouseInProperty, value); }
    }

    public static readonly DependencyProperty LineNumberProperty = DependencyProperty.Register(
        nameof(LineNumber), typeof(int), typeof(MainWindowLine), new PropertyMetadata(default(int)));

    public int LineNumber
    {
        get { return (int)GetValue(LineNumberProperty); }
        set { SetValue(LineNumberProperty, value); }
    }

    public static readonly DependencyProperty IsAllComponentsHidProperty = DependencyProperty.Register(
        nameof(IsAllComponentsHid), typeof(bool), typeof(MainWindowLine), new PropertyMetadata(default(bool)));

    public bool IsAllComponentsHid
    {
        get { return (bool)GetValue(IsAllComponentsHidProperty); }
        set { SetValue(IsAllComponentsHidProperty, value); }
    }

    public static readonly DependencyProperty IsLineFadedProperty = DependencyProperty.Register(
        nameof(IsLineFaded), typeof(bool), typeof(MainWindowLine), new PropertyMetadata(default(bool)));

    public bool IsLineFaded
    {
        get { return (bool)GetValue(IsLineFadedProperty); }
        set { SetValue(IsLineFadedProperty, value); }
    }

    private bool _isLoadCompleted = false;

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
        AddHandler(ComponentPresenter.ComponentVisibilityChangedEvent, new RoutedEventHandler(UpdateVisibilityState));
    }

    private void UpdateVisibilityState(object sender, RoutedEventArgs args)
    {
        Logger.LogTrace("ComponentVisibilityChangedEvent handled");
        IsAllComponentsHid = ComponentsService.CurrentComponents
            .Where(x => x.RelativeLineNumber == LineNumber)
            .FirstOrDefault(x => x.IsVisible) == null;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MainWindow.MousePosChanged += MainWindowOnMousePosChanged;
        MainWindow.RawInputEvent += MainWindowOnRawInputEvent;
        MainWindow.MainWindowAnimationEvent += MainWindowOnMainWindowAnimationEvent;
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        UpdateFadeStatus();

        _isLoadCompleted = true;

        Logger.LogDebug("LastStoryboardName = {}", LastStoryboardName);
        if (IsMainLine && LastStoryboardName != null && IsOverlayOpen)
        {
            BeginStoryboard(LastStoryboardName);
        }
    }


    private void OnUnloaded(object sender, RoutedEventArgs e)
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

    private Storyboard? BeginStoryboard(string name)
    {
        if (!_isLoadCompleted)
        {
            return null;
        }
        var a = (Storyboard)FindResource(name);
        GridWrapper?.BeginStoryboard(a);
        return a;
    }

    private void MainWindowOnMainWindowAnimationEvent(object? sender, MainWindowAnimationEventArgs e)
    {
        if (!IsMainLine)
        {
            return;
        }
        BeginStoryboard(e.StoryboardName);
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
        if (PresentationSource.FromVisual(this) == null)
        {
            return;
        }

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

    public override void OnApplyTemplate()
    {
        Logger.LogTrace("已应用控件模板");
        if (GetTemplateChild("PART_GridWrapper") is Grid wrapper)
        {
            GridWrapper = wrapper;
            wrapper.SizeChanged += WrapperOnSizeChanged;
        }

        Logger.LogDebug("LastStoryboardName = {}", LastStoryboardName);
        if (IsMainLine && LastStoryboardName != null && IsOverlayOpen)
        {
            BeginStoryboard(LastStoryboardName);
        }
        base.OnApplyTemplate();
    }

    private void WrapperOnSizeChanged(object sender, SizeChangedEventArgs e)
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

    private bool GetMouseStatusByPos(System.Drawing.Point ptr)
    {
        if (GridWrapper == null || PresentationSource.FromVisual(GridWrapper) == null)
        {
            return false;
        }
        MainWindow.GetCurrentDpi(out var dpiX, out var dpiY);
        var scale = MainWindow.ViewModel.Settings.Scale;
        //Debug.WriteLine($"Window: {Left * dpiX} {Top * dpiY};; Cursor: {ptr.X} {ptr.Y} ;; dpi: {dpiX}");
        var root = GridWrapper.PointToScreen(new Point(0, 0));
        var cx = root.X;
        var cy = root.Y;
        var cw = GridWrapper.ActualWidth * dpiX * scale;
        var ch = GridWrapper.ActualHeight * dpiY * scale;
        var cr = cx + cw;
        var cb = cy + ch;

        return (cx <= ptr.X && cy <= ptr.Y && ptr.X <= cr && ptr.Y <= cb);
    }
}