using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

// ReSharper disable once CheckNamespace
namespace Walterlv;

/// <summary>
///     The VisualTargetPresentationSource represents the root
///     of a visual subtree owned by a different thread that the
///     visual tree in which is is displayed.
/// </summary>
/// <remarks>
///     A HostVisual belongs to the same UI thread that owns the
///     visual tree in which it resides.
///     
///     A HostVisual can reference a VisualTarget owned by another
///     thread.
///     
///     A VisualTarget has a root visual.
///     
///     VisualTargetPresentationSource wraps the VisualTarget and
///     enables basic functionality like Loaded, which depends on
///     a PresentationSource being available.
/// </remarks>
public class VisualTargetPresentationSource : PresentationSource, IDisposable
{
    public VisualTargetPresentationSource(HostVisual hostVisual)
    {
        _visualTarget ??= new VisualTarget(hostVisual);
    }

    public override Visual RootVisual
    {
        get => _visualTarget.RootVisual;
        set
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("VisualTarget");
            }

            var oldRoot = _visualTarget.RootVisual;

            // Set the root visual of the VisualTarget.  This visual will
            // now be used to visually compose the scene.
            _visualTarget.RootVisual = value;

            // Hook the SizeChanged event on framework elements for all
            // future changed to the layout size of our root, and manually
            // trigger a size change.
            if (oldRoot is FrameworkElement oldRootFe)
            {
                oldRootFe.SizeChanged -= root_SizeChanged;
            }
            if (value is FrameworkElement rootFe)
            {
                rootFe.SizeChanged += root_SizeChanged;
                rootFe.DataContext = _dataContext;

                if (_propertyName != null)
                {
                    var myBinding = new Binding(_propertyName)
                    {
                        Source = _dataContext
                    };
                    rootFe.SetBinding(TextBlock.TextProperty, myBinding);
                }
            }

            // Tell the PresentationSource that the root visual has
            // changed.  This kicks off a bunch of stuff like the
            // Loaded event.
            RootChanged(oldRoot, value);

            // Kickoff layout...
            if (value is UIElement rootElement)
            {
                rootElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                rootElement.Arrange(new Rect(rootElement.DesiredSize));
            }
        }
    }

    public object DataContext
    {
        get => _dataContext;
        set
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("VisualTarget");
            }

            if (_dataContext == value)
            {
                return;
            }

            _dataContext = value;
            if (_visualTarget.RootVisual is FrameworkElement rootElement)
            {
                rootElement.DataContext = _dataContext;
            }
        }
    }

    public string PropertyName
    {
        get => _propertyName;
        set
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("VisualTarget");
            }

            _propertyName = value;

            if (_visualTarget.RootVisual is TextBlock rootElement)
            {
                if (!rootElement.CheckAccess())
                {
                    throw new InvalidOperationException("What?");
                }

                var myBinding = new Binding(_propertyName)
                {
                    Source = _dataContext
                };
                rootElement.SetBinding(TextBlock.TextProperty, myBinding);
            }
        }
    }

    public event SizeChangedEventHandler SizeChanged;

    public override bool IsDisposed => _isDisposed;

    protected override CompositionTarget GetCompositionTargetCore()
    {
        return _visualTarget;
    }

    private void root_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        SizeChanged?.Invoke(this, e);
    }

    private readonly VisualTarget _visualTarget;
    private object _dataContext;
    private string _propertyName;
    private bool _isDisposed;

    public void Dispose()
    {
        _visualTarget?.Dispose();
        _isDisposed = true;
    }
}