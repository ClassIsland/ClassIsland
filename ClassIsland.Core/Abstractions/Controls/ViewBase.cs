using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 通用视图基类。
/// </summary>
public abstract class ViewBase : ContentPage
{
    #region Fields

    private TaskCompletionSource DeactiveTcs { get; } = new();

    #endregion
    
    #region Events

    public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent = RoutedEvent.Register<ViewBase, RoutedEventArgs>(
        nameof(Closed), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    public EventHandler<WindowClosingEventArgs>? Closing;

    #endregion
    
    
    /// <summary>
    /// 关联的视图宿主。
    /// </summary>
    public IViewHost? AssociatedViewHost { get; internal set; }

    #region Lifetime
    
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

    internal bool ViewDeactivating()
    {
        return true;
    }

    internal void ViewDeactivated()
    {
        AssociatedViewHost = null;
        DeactiveTcs.TrySetResult();
    }

    #endregion

    #region PublicMethods
    /// <summary>
    /// 显示该视图。
    /// </summary>
    public virtual void Show()
    {
        if (AssociatedViewHost == null)
        {
            throw new InvalidOperationException("视图需要激活才能被显示。");
        }
        AssociatedViewHost.ShowView(this);
    }

    /// <summary>
    /// 以另一个视图为所有者显示视图。
    /// </summary>
    /// <param name="owner">所有者视图</param>
    public virtual void Show(ViewBase owner)
    {
        if (AssociatedViewHost == null)
        {
            throw new InvalidOperationException("视图需要激活才能被显示。");
        }
        AssociatedViewHost.ShowView(this);
    }

    /// <summary>
    /// 以模态显示并等待取消激活。
    /// </summary>
    /// <returns></returns>
    public virtual async Task ShowModal(ViewBase owner)
    {
        if (AssociatedViewHost == null)
        {
            throw new InvalidOperationException("视图需要激活才能被显示。");
        }
        await AssociatedViewHost.ShowViewModal(this, owner);
        await DeactiveTcs.Task;
    }
    
    /// <summary>
    /// 以模态显示并等待取消激活。
    /// </summary>
    /// <returns></returns>
    public virtual async Task<T> ShowModal<T>(ViewBase owner)
    {
        throw new NotImplementedException();
    }
    #endregion
    
    
}