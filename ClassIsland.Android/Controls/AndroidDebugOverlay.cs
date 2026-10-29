using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Avalonia.Rendering;
using ClassIsland.Android.Services;
using ClassIsland.Core;
using ClassIsland.Core.Enums;
using NativeView = global::Android.Views.View;

namespace ClassIsland.Android.Controls;

internal sealed class AndroidDebugOverlay : IDisposable
{
    private const string StatePrefix = "classisland.debugOverlay.";
    private readonly MainActivity _activity;
    private readonly FrameLayout _layer;
    private readonly ImageView _bubble;
    private readonly Rect _visibleBounds = new();
    private readonly Action _fadeAction;
    private readonly Action _sampleAction;
    private readonly int _touchSlop;
    private readonly List<Button> _actionButtons = [];
    private RendererDebugOverlays _overlays;
    private PopupWindow? _popup;
    private ScrollView? _panel;
    private TextView? _metricsText;
    private Button? _reloadButton;
    private AndroidProcessMetrics? _sampler;
    private float _xFraction = 1;
    private float _yFraction = 0.5f;
    private float _downX;
    private float _downY;
    private float _startX;
    private float _startY;
    private bool _dragging;
    private bool _gestureCanceled;
    private bool _operationInProgress;
    private bool _disposed;
    private int _sampleGeneration;

    internal AndroidDebugOverlay(MainActivity activity, Bundle? savedState)
    {
        _activity = activity;
        _fadeAction = FadeBubble;
        _sampleAction = SampleMetrics;
        _touchSlop = ViewConfiguration.Get(activity)!.ScaledTouchSlop;
        if (savedState != null)
        {
            _xFraction = savedState.GetFloat(StatePrefix + "x", 1);
            _yFraction = savedState.GetFloat(StatePrefix + "y", 0.5f);
            _overlays = (RendererDebugOverlays)savedState.GetInt(StatePrefix + "overlays");
        }

        _layer = new FrameLayout(activity);
        _layer.SetClipChildren(false);
        _layer.SetClipToPadding(false);
        _bubble = new ImageView(activity)
        {
            ContentDescription = "打开调试面板",
            Clickable = true,
            Focusable = true,
            ClipToOutline = true,
            Elevation = Dp(6),
            OutlineProvider = ViewOutlineProvider.Background
        };
        var background = new GradientDrawable();
        background.SetShape(ShapeType.Oval);
        background.SetColor(Color.White);
        _bubble.Background = background;
        _bubble.SetScaleType(ImageView.ScaleType.CenterCrop);
        _bubble.SetImageResource(Resource.Drawable.debug_floating_icon);
        if (_bubble.Drawable is BitmapDrawable icon)
        {
            icon.SetFilterBitmap(true);
            icon.SetAntiAlias(true);
            // Request mipmaps for the large reduction from the original image to 48 dp.
            icon.SetMipMap(true);
        }
        _bubble.Touch += BubbleOnTouch;
        _bubble.Click += BubbleOnClick;
        _layer.AddView(_bubble, new FrameLayout.LayoutParams(Dp(48), Dp(48), GravityFlags.Top | GravityFlags.Left));
        activity.AddContentView(_layer, new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
        _layer.ViewTreeObserver!.GlobalLayout += OnGlobalLayout;
        ApplyOverlays();
    }

    internal void ApplyOverlays()
    {
        if (!_disposed && _activity.ViewHostRoot is { } root)
        {
            root.RendererDiagnostics.DebugOverlays = _overlays;
        }
    }

    internal void SaveState(Bundle state)
    {
        _overlays = _activity.ViewHostRoot?.RendererDiagnostics.DebugOverlays ?? _overlays;
        state.PutFloat(StatePrefix + "x", _xFraction);
        state.PutFloat(StatePrefix + "y", _yFraction);
        state.PutInt(StatePrefix + "overlays", (int)_overlays);
    }

    internal void Resume()
    {
        RestoreOpacity();
        ScheduleFade();
    }

    internal void Pause()
    {
        _popup?.Dismiss();
        StopSampling();
        _bubble.RemoveCallbacks(_fadeAction);
        _bubble.Animate()?.Cancel();
    }

    private int Dp(float value) => (int)(value * _activity.Resources!.DisplayMetrics!.Density + 0.5f);

    private void OnGlobalLayout(object? sender, EventArgs e)
    {
        if (_disposed || _layer.Width == 0 || _layer.Height == 0)
        {
            return;
        }

        using var frame = new Rect();
        _layer.GetWindowVisibleDisplayFrame(frame);
        var location = new int[2];
        _layer.GetLocationOnScreen(location);
        var left = Math.Clamp(frame.Left - location[0], 0, _layer.Width);
        var top = Math.Clamp(frame.Top - location[1], 0, _layer.Height);
        var right = Math.Clamp(frame.Right - location[0], left, _layer.Width);
        var bottom = Math.Clamp(frame.Bottom - location[1], top, _layer.Height);
        if (right - left < Dp(48) || bottom - top < Dp(48) ||
            (_visibleBounds.Left == left && _visibleBounds.Top == top &&
             _visibleBounds.Right == right && _visibleBounds.Bottom == bottom))
        {
            return;
        }

        _visibleBounds.Set(left, top, right, bottom);
        PositionBubble(_xFraction, _yFraction);
        PositionPanel();
    }

    private (float Left, float Top, float Width, float Height) GetDragBounds()
    {
        var marginX = Math.Min(Dp(12), Math.Max(0, (_visibleBounds.Width() - Dp(48)) / 2));
        var marginY = Math.Min(Dp(12), Math.Max(0, (_visibleBounds.Height() - Dp(48)) / 2));
        return (_visibleBounds.Left + marginX, _visibleBounds.Top + marginY,
            Math.Max(0, _visibleBounds.Width() - Dp(48) - marginX * 2),
            Math.Max(0, _visibleBounds.Height() - Dp(48) - marginY * 2));
    }

    private void PositionBubble(float xFraction, float yFraction)
    {
        var bounds = GetDragBounds();
        var left = (int)MathF.Round(bounds.Left + bounds.Width * Math.Clamp(xFraction, 0, 1));
        var top = (int)MathF.Round(bounds.Top + bounds.Height * Math.Clamp(yFraction, 0, 1));
        var parameters = (FrameLayout.LayoutParams)_bubble.LayoutParameters!;
        if (parameters.LeftMargin == left && parameters.TopMargin == top)
        {
            return;
        }

        // Keep the native layout and outline bounds at the dragged position, rather than translating
        // an image whose layout rectangle remains at the origin and can be clipped by the renderer.
        parameters.LeftMargin = left;
        parameters.TopMargin = top;
        _bubble.LayoutParameters = parameters;
    }

    private void BubbleOnTouch(object? sender, NativeView.TouchEventArgs e)
    {
        if (e.Event is not { } motion)
        {
            return;
        }
        e.Handled = true;
        switch (motion.ActionMasked)
        {
            case MotionEventActions.Down:
                RestoreOpacity();
                _downX = motion.RawX;
                _downY = motion.RawY;
                _startX = _bubble.GetX();
                _startY = _bubble.GetY();
                _dragging = false;
                _gestureCanceled = false;
                break;
            case MotionEventActions.PointerDown:
                _gestureCanceled = true;
                break;
            case MotionEventActions.Move when !_gestureCanceled:
                var dx = motion.RawX - _downX;
                var dy = motion.RawY - _downY;
                _dragging |= dx * dx + dy * dy > _touchSlop * _touchSlop;
                if (_dragging)
                {
                    var bounds = GetDragBounds();
                    _xFraction = bounds.Width > 0 ? Math.Clamp((_startX + dx - bounds.Left) / bounds.Width, 0, 1) : 0;
                    _yFraction = bounds.Height > 0 ? Math.Clamp((_startY + dy - bounds.Top) / bounds.Height, 0, 1) : 0;
                    PositionBubble(_xFraction, _yFraction);
                }
                break;
            case MotionEventActions.Up:
                if (!_dragging && !_gestureCanceled)
                {
                    _bubble.PerformClick();
                }
                ScheduleFade();
                break;
            case MotionEventActions.Cancel:
                ScheduleFade();
                break;
        }
    }

    private void RestoreOpacity()
    {
        _bubble.RemoveCallbacks(_fadeAction);
        _bubble.Animate()?.Cancel();
        _bubble.Alpha = 1;
    }

    private void ScheduleFade()
    {
        _bubble.RemoveCallbacks(_fadeAction);
        if (!_disposed && _activity.IsForeground && _popup?.IsShowing != true)
        {
            _bubble.PostDelayed(_fadeAction, 5000);
        }
    }

    private void FadeBubble()
    {
        if (!_disposed && _activity.IsForeground && _popup?.IsShowing != true)
        {
            _bubble.Animate()?.Alpha(0.35f)?.SetDuration(300)?.Start();
        }
    }

    private void BubbleOnClick(object? sender, EventArgs e)
    {
        if (_disposed || !_activity.IsForeground)
        {
            return;
        }
        RestoreOpacity();
        if (_popup?.IsShowing == true)
        {
            _popup.Dismiss();
            return;
        }

        // Read the root again so keyboard shortcuts and the panel do not overwrite each other's flags.
        _overlays = _activity.ViewHostRoot?.RendererDiagnostics.DebugOverlays ?? _overlays;
        var content = new LinearLayout(_activity) { Orientation = Orientation.Vertical };
        content.SetPadding(Dp(16), Dp(12), Dp(16), Dp(12));
        var background = new GradientDrawable();
        background.SetCornerRadius(Dp(16));
        using var color = new TypedValue();
        _activity.Theme!.ResolveAttribute(global::Android.Resource.Attribute.ColorBackground, color, true);
        background.SetColor(new Color(color.Data));
        background.Alpha = 204;
        content.Background = background;
        content.AddView(CreateText($"Android 调试 · PID {(global::Android.OS.Process.MyPid())}", 18));
        _metricsText = CreateText("进程 RSS：正在采样…\n.NET 托管堆（估算）：正在采样…\nGC 已提交：正在采样…\n原生堆已分配：正在采样…\nCPU：采样中…", 14);
        content.AddView(_metricsText);
        content.AddView(CreateText("内存口径不同，不可直接相加；原生堆不含全部非托管映射。CPU 100% 表示占满一个核心。", 12));
        AddOverlaySwitch(content, "帧率（FPS）", RendererDebugOverlays.Fps);
        AddOverlaySwitch(content, "重绘区域", RendererDebugOverlays.DirtyRects);
        AddOverlaySwitch(content, "布局耗时图", RendererDebugOverlays.LayoutTimeGraph);
        AddOverlaySwitch(content, "渲染耗时图", RendererDebugOverlays.RenderTimeGraph);
        AddAction(content, "触发完整 GC", CollectGarbageAsync, successMessage: "完整 GC 已完成。");
        content.AddView(CreateText("重载视图宿主将清空导航和未保存的页面状态。", 12));
        _reloadButton = AddAction(content, "重载 AndroidViewHost", _activity.ReloadViewHostAsync,
            successMessage: "视图宿主已重载。");
        AddAction(content, "重启应用", () =>
        {
            AppBase.Current.Restart();
            return Task.CompletedTask;
        }, terminal: true);
        AddAction(content, "结束应用", () =>
        {
            AppBase.Current.Stop();
            return Task.CompletedTask;
        }, terminal: true);
        _panel = new ScrollView(_activity) { FillViewport = false };
        _panel.AddView(content);
        _popup = new PopupWindow(_panel, Dp(320), ViewGroup.LayoutParams.WrapContent, true)
        {
            OutsideTouchable = true,
            Elevation = Dp(8)
        };
        _popup.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
        _popup.DismissEvent += PopupOnDismiss;
        PositionPanel();
        UpdateActions();
        _sampler = new AndroidProcessMetrics();
        SampleMetrics();
    }

    private TextView CreateText(string text, float size)
    {
        var view = new TextView(_activity) { Text = text };
        view.SetTextSize(ComplexUnitType.Sp, size);
        view.SetPadding(0, Dp(4), 0, Dp(4));
        return view;
    }

    private void AddOverlaySwitch(LinearLayout content, string text, RendererDebugOverlays flag)
    {
        var toggle = new Switch(_activity)
        {
            Text = text,
            Checked = (_overlays & flag) != 0
        };
        toggle.SetMinimumHeight(Dp(48));
        toggle.CheckedChange += (_, e) =>
        {
            var current = _activity.ViewHostRoot?.RendererDiagnostics.DebugOverlays ?? _overlays;
            _overlays = e.IsChecked ? current | flag : current & ~flag;
            ApplyOverlays();
        };
        content.AddView(toggle);
    }

    private async Task CollectGarbageAsync()
    {
        await Task.Run(() =>
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        });
        if (!_disposed && _activity.IsForeground && _popup?.IsShowing == true)
        {
            StopSampling();
            _sampler = new AndroidProcessMetrics();
            SampleMetrics();
        }
    }

    private Button AddAction(LinearLayout content, string text, Func<Task> action, bool terminal = false,
        string? successMessage = null)
    {
        var button = new Button(_activity) { Text = text };
        button.SetAllCaps(false);
        button.Click += async (_, _) => await RunActionAsync(action, terminal, successMessage);
        _actionButtons.Add(button);
        content.AddView(button);
        return button;
    }

    private async Task RunActionAsync(Func<Task> action, bool terminal, string? successMessage)
    {
        if (_operationInProgress || _disposed)
        {
            return;
        }
        _operationInProgress = true;
        UpdateActions();
        try
        {
            await action();
            if (!terminal)
            {
                _operationInProgress = false;
                if (!_disposed && successMessage != null)
                {
                    Toast.MakeText(_activity, successMessage, ToastLength.Short)?.Show();
                }
            }
        }
        catch (Exception exception)
        {
            _operationInProgress = false;
            Log.Warn(nameof(AndroidDebugOverlay), exception.ToString());
            if (!_disposed)
            {
                Toast.MakeText(_activity, "操作未完成，请稍后重试；如页面无法恢复，请重启应用。", ToastLength.Long)?.Show();
            }
        }
        if (!_disposed)
        {
            UpdateActions();
        }
    }

    private void UpdateActions()
    {
        foreach (var button in _actionButtons)
        {
            button.Enabled = !_operationInProgress && AppBase.CurrentLifetime != ApplicationLifetime.Stopping;
        }
        if (_reloadButton != null)
        {
            _reloadButton.Enabled = !_operationInProgress && AppBase.CurrentLifetime == ApplicationLifetime.Running;
        }
    }

    private void PositionPanel()
    {
        if (_popup == null || _panel == null || _visibleBounds.IsEmpty)
        {
            return;
        }
        var margin = Dp(8);
        var width = Math.Min(Dp(320), Math.Max(1, _visibleBounds.Width() - margin * 2));
        var maxHeight = Math.Max(1, _visibleBounds.Height() - margin * 2);
        _panel.Measure(NativeView.MeasureSpec.MakeMeasureSpec(width, MeasureSpecMode.Exactly),
            NativeView.MeasureSpec.MakeMeasureSpec(maxHeight, MeasureSpecMode.AtMost));
        var height = Math.Min(maxHeight, _panel.MeasuredHeight);
        var bubbleParameters = (FrameLayout.LayoutParams)_bubble.LayoutParameters!;
        var x = Math.Clamp(bubbleParameters.LeftMargin - width + Dp(48),
            _visibleBounds.Left + margin, _visibleBounds.Right - margin - width);
        var y = Math.Clamp(bubbleParameters.TopMargin,
            _visibleBounds.Top + margin, _visibleBounds.Bottom - margin - height);
        var location = new int[2];
        _layer.GetLocationInWindow(location);
        if (_popup.IsShowing)
        {
            _popup.Update(x + location[0], y + location[1], width, height);
        }
        else
        {
            _popup.Width = width;
            _popup.Height = height;
            _popup.ShowAtLocation(_layer, GravityFlags.Top | GravityFlags.Left, x + location[0], y + location[1]);
        }
    }

    private async void SampleMetrics()
    {
        if (_disposed || !_activity.IsForeground || _popup?.IsShowing != true || _sampler == null)
        {
            return;
        }
        var generation = _sampleGeneration;
        var sampler = _sampler;
        var result = await Task.Run(sampler.Read);
        if (_disposed || generation != _sampleGeneration || !_activity.IsForeground || _popup?.IsShowing != true)
        {
            return;
        }
        _metricsText!.Text = result;
        UpdateActions();
        _bubble.PostDelayed(_sampleAction, 1000);
    }

    private void StopSampling()
    {
        ++_sampleGeneration;
        _bubble.RemoveCallbacks(_sampleAction);
        _sampler = null;
    }

    private void PopupOnDismiss(object? sender, EventArgs e)
    {
        StopSampling();
        if (_popup != null)
        {
            _popup.DismissEvent -= PopupOnDismiss;
            _popup.Dispose();
            _popup = null;
        }
        _panel = null;
        _metricsText = null;
        _reloadButton = null;
        _actionButtons.Clear();
        ScheduleFade();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        Pause();
        if (_layer.ViewTreeObserver is { IsAlive: true } observer)
        {
            observer.GlobalLayout -= OnGlobalLayout;
        }
        _bubble.Touch -= BubbleOnTouch;
        _bubble.Click -= BubbleOnClick;
        (_layer.Parent as ViewGroup)?.RemoveView(_layer);
        _bubble.SetImageDrawable(null);
        _bubble.Dispose();
        _layer.Dispose();
        _visibleBounds.Dispose();
    }
}
