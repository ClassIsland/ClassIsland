using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Models.UI;
using ClassIsland.Core.Services.UI;
using ClassIsland.Platforms.Abstraction.Enums;

namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 通用视图基类。
/// </summary>
public abstract class ViewBase : ContentPage
{
    #region Fields

    private TaskCompletionSource? DeActiveTcs { get; set; }

    private bool _isShowed = false;

    public static readonly StyledProperty<object?> ResultProperty = AvaloniaProperty.Register<ViewBase, object?>(
        nameof(Result));

    public object? Result
    {
        get => GetValue(ResultProperty);
        set => SetValue(ResultProperty, value);
    }

    public static readonly StyledProperty<double> HostWidthProperty = AvaloniaProperty.Register<ViewBase, double>(
        nameof(HostWidth), 600.0);

    public double HostWidth
    {
        get => GetValue(HostWidthProperty);
        set => SetValue(HostWidthProperty, value);
    }

    public static readonly StyledProperty<double> HostHeightProperty = AvaloniaProperty.Register<ViewBase, double>(
        nameof(HostHeight), 400.0);

    public double HostHeight
    {
        get => GetValue(HostHeightProperty);
        set => SetValue(HostHeightProperty, value);
    }

    public static readonly StyledProperty<PixelPoint> HostPositionProperty = AvaloniaProperty.Register<ViewBase, PixelPoint>(
        nameof(HostPosition));

    public PixelPoint HostPosition
    {
        get => GetValue(HostPositionProperty);
        set => SetValue(HostPositionProperty, value);
    }

    public static readonly StyledProperty<WindowState> HostWindowStateProperty = AvaloniaProperty.Register<ViewBase, WindowState>(
        nameof(HostWindowState), WindowState.Normal);

    public WindowState HostWindowState
    {
        get => GetValue(HostWindowStateProperty);
        set => SetValue(HostWindowStateProperty, value);
    }

    public static readonly StyledProperty<WindowStartupLocation> HostStartupLocationProperty = AvaloniaProperty.Register<ViewBase, WindowStartupLocation>(
        nameof(HostStartupLocation));

    public WindowStartupLocation HostStartupLocation
    {
        get => GetValue(HostStartupLocationProperty);
        set => SetValue(HostStartupLocationProperty, value);
    }
    
    public static readonly StyledProperty<bool> UseInlineHeaderProperty = AvaloniaProperty.Register<ViewBase, bool>(
        nameof(UseInlineHeader), true);

    public bool UseInlineHeader
    {
        get => GetValue(UseInlineHeaderProperty);
        set => SetValue(UseInlineHeaderProperty, value);
    }

    public static readonly StyledProperty<bool> ShowAsDialogProperty = AvaloniaProperty.Register<ViewBase, bool>(
        nameof(ShowAsDialog));

    public bool ShowAsDialog
    {
        get => GetValue(ShowAsDialogProperty);
        set => SetValue(ShowAsDialogProperty, value);
    }

    public static readonly StyledProperty<bool> CanResizeProperty = AvaloniaProperty.Register<ViewBase, bool>(
        nameof(CanResize), true);

    public bool CanResize
    {
        get => GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    public static readonly StyledProperty<SizeToContent> SizeToContentProperty =
        Window.SizeToContentProperty.AddOwner<ViewBase>(
            new StyledPropertyMetadata<SizeToContent>(SizeToContent.Manual));

    public SizeToContent SizeToContent
    {
        get => GetValue(SizeToContentProperty);
        set => SetValue(SizeToContentProperty, value);
    }

    public static readonly StyledProperty<bool> CanMaximizeProperty =
        Window.CanMaximizeProperty.AddOwner<ViewBase>(new StyledPropertyMetadata<bool>(true));

    public bool CanMaximize
    {
        get => GetValue(CanMaximizeProperty);
        set => SetValue(CanMaximizeProperty, value);
    }
    
    public bool ShowedOnce { get; private set; }

    public TopLevel? TopLevel => TopLevel.GetTopLevel(this);

    public static readonly StyledProperty<AvaloniaList<WindowFeatures>> HostFeaturesProperty = AvaloniaProperty.Register<ViewBase, AvaloniaList<WindowFeatures>>(
        nameof(HostFeatures));

    public AvaloniaList<WindowFeatures> HostFeatures
    {
        get => GetValue(HostFeaturesProperty);
        set => SetValue(HostFeaturesProperty, value);
    }

    
    #endregion
    
    #region Events

    public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent = RoutedEvent.Register<ViewBase, RoutedEventArgs>(
        nameof(Closed), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    public event EventHandler<ViewClosingEventArgs>? Closing;
    

    #endregion
    
    
    /// <summary>
    /// 关联的视图宿主。
    /// </summary>
    public IViewHost? AssociatedViewHost { get; internal set; }

    #region Lifetime

    /// <inheritdoc />
    public ViewBase()
    {
        SetCurrentValue(HostFeaturesProperty, new AvaloniaList<WindowFeatures>());
        Navigating += OnNavigating;
    }

    private async Task OnNavigating(NavigatingFromEventArgs arg)
    {
        if (arg.NavigationType is NavigationType.Insert or NavigationType.Push or NavigationType.PushModal)
        {
            return;
        }

        if (!_isShowed)
        {
            return;
        }

        if (InvokeClosingEvent(WindowCloseReason.Undefined, false, true))
        {
            arg.Cancel = true;
        }
    }

    internal bool ViewActivating(IViewHost viewHost)
    {
        if (AssociatedViewHost != null)
        {
            throw new InvalidOperationException("视图已被激活到视图宿主，不可被重复激活。");
        }

        return true;
    }
    
    internal void ViewActivated(IViewHost viewHost)
    {
        AssociatedViewHost = viewHost;
    }

    internal bool ViewDeactivating(WindowCloseReason reason, bool isProgrammatic, bool isCancelable)
    {
        return !InvokeClosingEvent(reason, isProgrammatic, isCancelable);
    }

    private bool InvokeClosingEvent(WindowCloseReason reason, bool isProgrammatic, bool isCancelable)
    {
        var eventArgs = new ViewClosingEventArgs(reason, isProgrammatic, isCancelable);
        Closing?.Invoke(this, eventArgs);
        return eventArgs.Cancel;
    }

    internal void ViewDeactivated()
    {
        var wasShowed = _isShowed;
        var deActiveTcs = DeActiveTcs;

        if (!wasShowed && AssociatedViewHost == null && deActiveTcs == null)
        {
            return;
        }

        AssociatedViewHost = null;
        _isShowed = false;
        DeActiveTcs = null;
        deActiveTcs?.TrySetResult();

        if (wasShowed)
        {
            RaiseEvent(new RoutedEventArgs(ClosedEvent));
        }
    }

    private void ShowCore(ViewBase? owner = null, Window? windowOwner = null, bool modal = false)
    {
        if (AssociatedViewHost == null)
        {
            throw new InvalidOperationException("只有在该视图被激活到视图宿主后才能显示此视图。");
        }

        if (_isShowed)
        {
            throw new InvalidOperationException("视图已被显示时不能再次被显示。");
        }

        if (windowOwner != null && modal)
        {
            _ = AssociatedViewHost.ShowViewModal(this, windowOwner);
        }
        else if (owner != null && modal)
        {
            _ = AssociatedViewHost.ShowViewModal(this, owner);
        }
        else
        {
            _ = AssociatedViewHost.ShowView(this, owner);
        }
        _isShowed = true;
        ShowedOnce = true;
    }

    private async Task ShowModalCore(ViewBase? owner = null, Window? windowOwner = null)
    {
        if (AssociatedViewHost == null)
        {
            ViewManagementService.Instance.ActivateView(this);
        }

        if (_isShowed)
        {
            throw new InvalidOperationException("视图已被显示时不能再次被显示。");
        }

        var deActiveTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        DeActiveTcs = deActiveTcs;
        try
        {
            ShowCore(owner, windowOwner, true);
            await deActiveTcs.Task;
        }
        catch
        {
            if (ReferenceEquals(DeActiveTcs, deActiveTcs))
            {
                DeActiveTcs = null;
            }
            throw;
        }
    }

    #endregion

    #region PublicMethods

    /// <summary>
    /// 尝试打开此视图，或将已打开的视图显示到前台。
    /// </summary>
    /// <remarks>如果视图已经显示，不会抛出异常，而是将视图显示到最前端。</remarks>
    /// <param name="owner">所有者视图</param>
    public virtual void Open(ViewBase? owner = null)
    {
        if (_isShowed && AssociatedViewHost != null)
        {
            // 如果不等一会再激活，可能会出现从托盘菜单打开的界面不激活的问题。
            Dispatcher.Post(() =>
            {
                AssociatedViewHost.Activate();
            });
            return;
        }
        Show(owner);
    }
    
    /// <summary>
    /// 以另一个视图为所有者显示视图。
    /// </summary>
    /// <param name="owner">所有者视图</param>
    public virtual void Show(ViewBase? owner = null)
    {
        if (AssociatedViewHost == null)
        {
            ViewManagementService.Instance.ActivateView(this);
        }
        ShowCore(owner);
    }

    /// <summary>
    /// 以模态显示并等待取消激活。
    /// </summary>
    /// <returns></returns>
    public virtual async Task ShowModal(ViewBase? owner=null)
    {
        await ShowModalCore(owner);
    }
    
    /// <summary>
    /// 以模态显示并等待取消激活。
    /// </summary>
    /// <returns></returns>
    public virtual async Task<T> ShowModal<T>(ViewBase? owner=null)
    {
        await ShowModalCore(owner);
        return (T)Result!;
    }

    /// <summary>
    /// 以原生窗口为所有者模态显示并等待取消激活。
    /// </summary>
    /// <param name="owner">所有者窗口。</param>
    public virtual async Task ShowModal(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        await ShowModalCore(windowOwner: owner);
    }

    /// <summary>
    /// 以原生窗口为所有者模态显示并等待取消激活。
    /// </summary>
    /// <param name="owner">所有者窗口。</param>
    /// <typeparam name="T">结果类型。</typeparam>
    public virtual async Task<T> ShowModal<T>(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        await ShowModalCore(windowOwner: owner);
        return (T)Result!;
    }

    /// <summary>
    /// 隐藏当前视图。
    /// </summary>
    public virtual void Hide()
    {
        if (AssociatedViewHost == null)
        {
            throw new InvalidOperationException("只有在该视图被激活到视图宿主后才能隐藏此视图。");
        }

        AssociatedViewHost.HideView(this);
    }
    
    /// <summary>
    /// 隐藏当前视图，效果与 <see cref="Hide"/> 等价。
    /// </summary>
    public void Close() => Hide();
    
    #endregion


}
