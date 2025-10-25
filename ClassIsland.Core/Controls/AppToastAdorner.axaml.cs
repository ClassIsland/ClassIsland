using System.Collections.ObjectModel;
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
        InitializeComponent();
    }

    private void TopLevelOnClosed(object? sender, EventArgs e)
    {
        TopLevel.Closed -= TopLevelOnClosed;
        TopLevel.RemoveHandler(ShowToastEvent, OnShowToast);
    }

    private void OnShowToast(object? sender, ShowToastEventArgs e)
    {
        Messages.Insert(0, e.Message);
        e.Message.ClosedCancellationTokenSource.Token.Register(() =>
        {
            DispatcherTimer.RunOnce(() => Messages.Remove(e.Message), TimeSpan.FromSeconds(0.3));
        });
        if (e.Message.AutoClose)
        {
            DispatcherTimer.RunOnce(() => e.Message.Close(), e.Message.Duration);
        }
    }

    [RelayCommand]
    private void CloseToast(ToastMessage message)
    {
        message.Close();
    }
}