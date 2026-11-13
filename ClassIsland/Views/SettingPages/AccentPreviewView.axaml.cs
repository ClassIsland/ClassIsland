using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.ViewModels.SettingsPages;

namespace ClassIsland.Views.SettingPages;

public partial class AccentPreviewView : ViewBase, IDisposable
{
    public static readonly DirectProperty<AccentPreviewView, string?> RefreshErrorProperty =
        AvaloniaProperty.RegisterDirect<AccentPreviewView, string?>(nameof(RefreshError), view => view.RefreshError);

    private readonly CancellationTokenSource _cancellation = new();
    private string? _refreshError;
    private bool _disposed;

    public InterfaceSettingsViewModel ViewModel { get; }

    public string? RefreshError
    {
        get => _refreshError;
        private set => SetAndRaise(RefreshErrorProperty, ref _refreshError, value);
    }

    public AccentPreviewView(InterfaceSettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        UpdatePreview();
        ViewModel.AccentPicker.PropertyChanged += OnPickerChanged;
    }

    private void OnPickerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAccentPickerService.LastResult))
            UpdatePreview();
    }

    private void UpdatePreview()
    {
        var previous = PreviewImage.Source as Bitmap;
        if (ViewModel.AccentPicker.LastResult?.PreviewPng is { IsEmpty: false } png)
        {
            using var stream = new MemoryStream(png.ToArray(), writable: false);
            PreviewImage.Source = new Bitmap(stream);
        }
        else
        {
            PreviewImage.Source = null;
        }
        previous?.Dispose();
    }

    private async void Refresh_OnClick(object? sender, RoutedEventArgs e)
    {
        var token = _cancellation.Token;
        try
        {
            RefreshError = null;
            await ViewModel.RefreshAccentAsync(token);
            RefreshError = ViewModel.AccentError;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        ViewModel.AccentPicker.PropertyChanged -= OnPickerChanged;
        _cancellation.Cancel();
        _cancellation.Dispose();
        var bitmap = PreviewImage.Source as Bitmap;
        PreviewImage.Source = null;
        bitmap?.Dispose();
    }
}
