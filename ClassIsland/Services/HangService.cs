using System.Threading.Tasks;
using System;
using System.Timers;
using System.Windows.Threading;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Services;

public class HangService : ObservableRecipient
{
    private bool _isHang = false;
    private bool _isChecking = false;

    public bool IsHang
    {
        get => _isHang;
        set
        {
            if (value == _isHang) return;
            _isHang = value;
            OnPropertyChanged();
        }
    }

    public bool IsChecking
    {
        get => _isChecking;
        set
        {
            if (value == _isChecking) return;
            _isChecking = value;
            OnPropertyChanged();
        }
    }

    private Timer Timer { get; } = new()
    {
        Interval = 500
    };

    public HangService()
    {
        Timer.Elapsed += TimerOnElapsed;
        Timer.Start();
    }

    public void AssumeHang()
    {
        IsHang = true;
        _ = CheckDispatcherHangAsync();
    }

    private async void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        await CheckDispatcherHangAsync();
    }

    private async Task CheckDispatcherHangAsync()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || IsChecking)
        {
            return;
        }
        var taskCompletionSource = new TaskCompletionSource<bool>();
        _ = dispatcher.InvokeAsync(() => taskCompletionSource.TrySetResult(true));
        IsChecking = true;
        await Task.WhenAny(taskCompletionSource.Task, Task.Delay(TimeSpan.FromMilliseconds(50)));
        IsChecking = false;
        // 如果任务还没完成，就是界面卡了
        IsHang = taskCompletionSource.Task.IsCompleted is false;
        Timer.Start();
    }
}