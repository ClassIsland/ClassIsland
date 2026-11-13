using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Views.SettingPages;

public partial class WallpaperWindowPicker : ViewBase, IDisposable
{
    private readonly IScreenshotService _screenshots;
    private readonly ObservableCollection<WindowCandidate> _windows = [];
    private CancellationTokenSource? _loading;
    private bool _disposed;

    public WallpaperWindowPicker(IScreenshotService screenshots, string? selectedClassName)
    {
        _screenshots = screenshots;
        InitializeComponent();
        ClassNameInput.Text = selectedClassName;
        Candidates.ItemsSource = _windows;
        Loaded += async (_, _) => await ReloadAsync();
    }

    public string SelectedClassName => ClassNameInput.Text?.Trim() ?? "";

    private void Cancel_OnClick(object? sender, RoutedEventArgs e) => Close();

    private void Confirm_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = SelectedClassName;
        Close();
    }

    private async void Filter_OnChanged(object? sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            await ReloadAsync();
    }

    private async void Refresh_OnClick(object? sender, RoutedEventArgs e) => await ReloadAsync();

    private void Candidates_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Candidates.SelectedItem is WindowCandidate window)
            ClassNameInput.Text = window.ClassName;
    }

    private async Task ReloadAsync()
    {
        if (_disposed)
            return;
        _loading?.Cancel();
        using var cancellation = new CancellationTokenSource();
        _loading = cancellation;
        Loading.IsVisible = true;
        Status.Text = "";
        ClearWindows();
        try
        {
            await foreach (var window in _screenshots.EnumerateWindowsAsync(FullScreenOnly.IsChecked == true, cancellation.Token))
            {
                cancellation.Token.ThrowIfCancellationRequested();
                Bitmap? thumbnail = null;
                if (!window.ThumbnailPng.IsEmpty)
                {
                    using var stream = new MemoryStream(window.ThumbnailPng.ToArray(), writable: false);
                    thumbnail = new Bitmap(stream);
                }
                _windows.Add(new WindowCandidate(window.Title, window.ClassName, thumbnail));
            }
            if (_windows.Count == 0)
                Status.Text = "没有找到符合条件的窗口。可以取消筛选，或直接输入窗口类名。";
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            IAppHost.TryGetService<ILogger<WallpaperWindowPicker>>()?.LogWarning(exception, "无法列出壁纸窗口。");
            if (!cancellation.IsCancellationRequested)
                Status.Text = "无法加载窗口。请刷新列表，或直接输入窗口类名。";
        }
        finally
        {
            if (ReferenceEquals(_loading, cancellation))
            {
                _loading = null;
                Loading.IsVisible = false;
            }
        }
    }

    private void ClearWindows()
    {
        var previous = new System.Collections.Generic.List<WindowCandidate>(_windows);
        _windows.Clear();
        foreach (var window in previous)
            window.Thumbnail?.Dispose();
    }

    public void Dispose()
    {
        _disposed = true;
        _loading?.Cancel();
        ClearWindows();
    }

    public sealed record WindowCandidate(string Title, string ClassName, Bitmap? Thumbnail);
}
