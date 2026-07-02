using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Models.UI;
using ClassIsland.Core.Services.UI;

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
        AssociatedViewHost = null;
        DeActiveTcs?.TrySetResult();
        _isShowed = false;
    }

    private void ShowCore(ViewBase? owner = null, bool modal = false)
    {
        if (AssociatedViewHost == null)
        {
            throw new InvalidOperationException("只有在该视图被激活到视图宿主后才能显示此视图。");
        }

        if (_isShowed)
        {
            throw new InvalidOperationException("视图已被显示时不能再次被显示。");
        }

        if (owner != null)
        {
            AssociatedViewHost.ShowViewModal(this, owner);
        }
        else
        {
            AssociatedViewHost.ShowView(this, owner);
        }
        _isShowed = true;
    }

    #endregion

    #region PublicMethods

    /// <summary>
    /// 尝试打开此视图，或将已打开的视图显示到前台。
    /// </summary>
    /// <remarks>如果视图已经显示，不会抛出异常，而是将视图显示到最前端。</remarks>
    /// <param name="owner">所有者视图</param>
    /// <returns>视图是否显示成功。返回 false 时代表视图已经打开，仅将视图显示到了前台。</returns>
    public virtual bool Open(ViewBase? owner = null)
    {
        if (_isShowed && AssociatedViewHost != null)
        {
            AssociatedViewHost.Activate();
            return false;
        }
        Show(owner);
        return true;
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
    public virtual async Task ShowModal(ViewBase owner)
    {
        if (AssociatedViewHost == null)
        {
            ViewManagementService.Instance.ActivateView(this);
        }
        ShowCore(owner, true);
        DeActiveTcs = new TaskCompletionSource();
        await DeActiveTcs.Task;
    }
    
    /// <summary>
    /// 以模态显示并等待取消激活。
    /// </summary>
    /// <returns></returns>
    public virtual async Task<T> ShowModal<T>(ViewBase owner)
    {
        if (AssociatedViewHost == null)
        {
            ViewManagementService.Instance.ActivateView(this);
        }
        ShowCore(owner, true);
        DeActiveTcs = new TaskCompletionSource();
        await DeActiveTcs.Task;
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
    #endregion
    
    
}