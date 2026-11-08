using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ClassIsland.Core.Models.UI;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.Core.Controls;

public partial class AppToastAdorner : UserControl
{
    private readonly Dictionary<ToastMessage, ToastLifetime> _lifetimes = new();
    private TopLevel TopLevel { get; }
    public ObservableCollection<ToastMessage> Messages { get; } = [];
    
    public static readonly RoutedEvent<ShowToastEventArgs> ShowToastEvent =
        RoutedEvent.Register<AppToastAdorner, ShowToastEventArgs>(nameof(ShowToast), RoutingStrategies.Bubble);

    // Provide CLR accessors for the event
    public event EventHandler<ShowToastEventArgs> ShowToast
    { 
        add => AddHandler(ShowToastEvent, value);
        remove => RemoveHandler(ShowToastEvent, value);
    }
    
    public AppToastAdorner(TopLevel topLevel)
    {
        TopLevel = topLevel;
        topLevel.AddHandler(ShowToastEvent, OnShowToast);
        topLevel.Closed += TopLevelOnClosed;
        Messages.CollectionChanged += (_, _) =>
        {
            foreach (var message in _lifetimes.Keys.Where(x => !Messages.Contains(x)).ToArray())
            {
                _lifetimes.Remove(message, out var lifetime);
                lifetime?.Dispose();
            }
        };
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        TopLevel.RemoveHandler(ShowToastEvent, OnShowToast);
        TopLevel.Closed -= TopLevelOnClosed;
        TopLevel.AddHandler(ShowToastEvent, OnShowToast);
        TopLevel.Closed += TopLevelOnClosed;
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        TopLevelOnClosed(null, EventArgs.Empty);
        base.OnDetachedFromVisualTree(e);
    }

    private void TopLevelOnClosed(object? sender, EventArgs e)
    {
        TopLevel.Closed -= TopLevelOnClosed;
        TopLevel.RemoveHandler(ShowToastEvent, OnShowToast);
        foreach (var lifetime in _lifetimes.Values)
        {
            lifetime.Dispose();
        }
        _lifetimes.Clear();
        Messages.Clear();
    }

    private void OnShowToast(object? sender, ShowToastEventArgs e)
    {
        var message = e.Message;
        if (!message.IsOpen || _lifetimes.ContainsKey(message))
        {
            return;
        }
        var lifetime = new ToastLifetime(message);
        _lifetimes.Add(message, lifetime);
        Messages.Insert(0, message);
        lifetime.ClosedRegistration = message.ClosedCancellationTokenSource.Token.Register(() =>
            Dispatcher.UIThread.Post(() =>
            {
                if (!_lifetimes.ContainsKey(message)) return;
                lifetime.Pause();
                lifetime.RemovalTimer = DispatcherTimer.RunOnce(() =>
                {
                    _lifetimes.Remove(message);
                    lifetime.Dispose();
                    Messages.Remove(message);
                }, TimeSpan.FromMilliseconds(300));
            }));
        lifetime.Resume();
    }

    internal void SetToastInteracting(ToastMessage message, bool interacting)
    {
        if (!_lifetimes.TryGetValue(message, out var lifetime)) return;
        if (interacting)
        {
            lifetime.Pause();
        }
        else
        {
            lifetime.Resume();
        }
    }

    [RelayCommand]
    private void CloseToast(ToastMessage message)
    {
        message.Close();
    }

    private sealed class ToastLifetime(ToastMessage message) : IDisposable
    {
        private TimeSpan _remaining = message.Duration;
        private long _startedAt;
        private IDisposable? _timer;

        public CancellationTokenRegistration ClosedRegistration { get; set; }
        public IDisposable? RemovalTimer { get; set; }

        public void Pause()
        {
            if (_timer == null) return;
            _timer.Dispose();
            _timer = null;
            _remaining -= Stopwatch.GetElapsedTime(_startedAt);
        }

        public void Resume()
        {
            if (_timer != null || !message.AutoClose || !message.IsOpen) return;
            _startedAt = Stopwatch.GetTimestamp();
            _timer = DispatcherTimer.RunOnce(() =>
            {
                _timer = null;
                if (message.IsOpen) message.Close();
            }, _remaining > TimeSpan.Zero ? _remaining : TimeSpan.Zero);
        }

        public void Dispose()
        {
            _timer?.Dispose();
            RemovalTimer?.Dispose();
            ClosedRegistration.Dispose();
        }
    }
}
