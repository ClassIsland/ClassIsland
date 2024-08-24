using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

using Walterlv.Threading;

using DispatcherDictionary = System.Collections.Concurrent.ConcurrentDictionary<System.Windows.Threading.Dispatcher, Walterlv.Threading.DispatcherAsyncOperation<System.Windows.Threading.Dispatcher>>;

namespace Walterlv.Windows;

[ContentProperty(nameof(Child))]
public class AsyncBox : FrameworkElement
{
    /// <summary>
    /// 保存外部 UI 线程和与其关联的异步 UI 线程。
    /// 例如主 UI 线程对应一个 AsyncBox 专用的 UI 线程；外面可能有另一个 UI 线程，那么对应另一个 AsyncBox 专用的 UI 线程。
    /// </summary>
    private static readonly DispatcherDictionary RelatedAsyncDispatchers = new DispatcherDictionary();

    private UIElement _child;

    private readonly HostVisual _hostVisual;

    private VisualTargetPresentationSource _targetSource;

    private UIElement _loadingView;

    private readonly ContentPresenter _contentPresenter;

    private bool _isChildReadyToLoad;

    private Type _loadingViewType;

    public event EventHandler? LoadingViewLoaded;

    public AsyncBox()
    {
        _hostVisual = new HostVisual();
        _contentPresenter = new ContentPresenter();
        Loaded += OnLoaded;
    }

    public UIElement Child
    {
        get => _child;
        set
        {
            if (Equals(_child, value)) return;

            if (value != null)
            {
                RemoveLogicalChild(value);
            }

            _child = value;

            if (_isChildReadyToLoad)
            {
                ActivateChild();
            }
        }
    }

    public async Task ShutdownUiDispatcherAsync()
    {
        var dispatcher = await GetAsyncDispatcherAsync();
        dispatcher.InvokeShutdown();
    }

    public Type LoadingViewType
    {
        get
        {
            if (_loadingViewType == null)
            {
                throw new InvalidOperationException(
                    $"在 {nameof(AsyncBox)} 显示之前，必须先为 {nameof(LoadingViewType)} 设置一个 {nameof(UIElement)} 作为 Loading 视图。");
            }

            return _loadingViewType;
        }
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(LoadingViewType));
            }

            if (_loadingViewType != null)
            {
                throw new ArgumentException($"{nameof(LoadingViewType)} 只允许被设置一次。", nameof(value));
            }

            _loadingViewType = value;
        }
    }

    /// <summary>
    /// 返回一个可等待的用于显示异步 UI 的后台 UI 线程调度器。
    /// </summary>
    private DispatcherAsyncOperation<Dispatcher> GetAsyncDispatcherAsync() => RelatedAsyncDispatchers.GetOrAdd(
        Dispatcher, dispatcher => UIDispatcher.RunNewAsync("AsyncBox"));

    private UIElement CreateLoadingView()
    {
        var instance = Activator.CreateInstance(LoadingViewType);
        if (instance is UIElement element)
        {
            return element;
        }

        throw new InvalidOperationException($"{LoadingViewType} 必须是 {nameof(UIElement)} 类型");
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DesignerProperties.GetIsInDesignMode(this))
        {

            AddVisualChild(_contentPresenter);
            AddVisualChild(_hostVisual);
            ActivateChild();

            return;
        }

        var dispatcher = await GetAsyncDispatcherAsync();
        _loadingView = await dispatcher.InvokeAsync(() =>
        {
            var loadingView = CreateLoadingView();
            _targetSource = new VisualTargetPresentationSource(_hostVisual)
            {
                RootVisual = loadingView
            };
            return loadingView;
        });
        AddVisualChild(_contentPresenter);
        AddVisualChild(_hostVisual);

        await LayoutAsync();
        await Dispatcher.Yield(DispatcherPriority.Background);

        _isChildReadyToLoad = true;
        ActivateChild();
        LoadingViewLoaded?.Invoke(this, EventArgs.Empty);
    }

    private void ActivateChild()
    {
        var child = Child;
        if (child != null)
        {
            _contentPresenter.Content = child;
            AddLogicalChild(child);
            InvalidateMeasure();
        }
    }

    private async Task LayoutAsync()
    {
        var dispatcher = await GetAsyncDispatcherAsync();
        await dispatcher.InvokeAsync(() =>
        {
            if (_loadingView != null)
            {
                _loadingView.Measure(RenderSize);
                _loadingView.Arrange(new Rect(RenderSize));
            }
        });
    }

    protected override int VisualChildrenCount => _loadingView != null ? 2 : 0;

    protected override Visual GetVisualChild(int index)
    {
        switch (index)
        {
            case 0:
                return _contentPresenter;
            case 1:
                return _hostVisual;
            default:
                return null;
        }
    }

    protected override IEnumerator LogicalChildren
    {
        get
        {
            if (_isChildReadyToLoad)
            {
                yield return _contentPresenter;
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_isChildReadyToLoad)
        {
            _contentPresenter.Measure(availableSize);
            return _contentPresenter.DesiredSize;
        }

        var size = base.MeasureOverride(availableSize);
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_isChildReadyToLoad)
        {
            _contentPresenter.Arrange(new Rect(finalSize));
            var renderSize = _contentPresenter.RenderSize;
            LayoutAsync().ConfigureAwait(false);
            return renderSize;
        }

        var size = base.ArrangeOverride(finalSize);
        LayoutAsync().ConfigureAwait(false);
        return size;
    }
}