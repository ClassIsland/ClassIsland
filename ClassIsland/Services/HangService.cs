using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using ClassIsland.Core.Abstractions.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Services;

public class HangService : ObservableRecipient, IHangService
{
    private bool _isHang = false;
    private bool _isChecking = false;
    private const int CHECK_INTERVAL_MS = 500;
    private const int HANG_MS = 100;


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
        Interval = CHECK_INTERVAL_MS
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
        await Task.WhenAny(taskCompletionSource.Task, Task.Delay(TimeSpan.FromMilliseconds(HANG_MS)));
        IsChecking = false;
        // 如果任务还没完成，就是界面卡了
        IsHang = taskCompletionSource.Task.IsCompleted is false;
        Timer.Start();
    }
}