using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using ClassIsland.Controls;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Models.Logging;
using ClassIsland.Services.Logging;
using ClassIsland.ViewModels;

using Microsoft.Extensions.Logging;

namespace ClassIsland.Views;

/// <summary>
/// AppLogsWindow.xaml 的交互逻辑
/// </summary>
public partial class AppLogsWindow : MyWindow
{
    public AppLogsViewModel ViewModel { get; } = new();

    public AppLogService AppLogService { get; }

    private bool _isOpened = false;

    public AppLogsWindow(AppLogService appLogService)
    {
        AppLogService = appLogService;
        InitializeComponent();
        DataContext = this;
        AppLogService.Logs.CollectionChanged += LogsOnCollectionChanged;
    }

    public void Open()
    {
        if (!_isOpened)
        {
            _isOpened = true;
            Show();
        }
        else
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
        }
    }

    private void LogsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        
    }

    private void AppLogsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        _isOpened = false;
    }

    private void MainListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count <= 0)
            return;
        var o = e.AddedItems[0];
        if (o == null)
            return;
        
    }

    private void Buttontest_ScrollIntoView_OnClick(object sender, RoutedEventArgs e)
    {
        MainListView.ScrollIntoView(AppLogService.Logs[^1]);
    }

    private void LogsSource_OnFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not LogEntry i) 
            return;
        bool c1 = ViewModel.IsFilteredCritical && i.LogLevel == LogLevel.Critical ||
                  ViewModel.IsFilteredError && i.LogLevel == LogLevel.Error ||
                  ViewModel.IsFilteredWarning && i.LogLevel == LogLevel.Warning ||
                  ViewModel.IsFilteredInfo && i.LogLevel == LogLevel.Information ||
                  ViewModel.IsFilteredDebug && i.LogLevel == LogLevel.Debug ||
                  ViewModel.IsFilteredTrace && i.LogLevel == LogLevel.Trace;
        if (string.IsNullOrWhiteSpace(ViewModel.FilterText))
        {
            e.Accepted = c1;
        }
        else
        {
            e.Accepted = c1 && (i.Message.Contains(ViewModel.FilterText) || i.CategoryName.Contains(ViewModel.FilterText));
        }
    }

    private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshView();
    }

    private void RefreshView()
    {
        if (FindResource("LogsSource") is CollectionViewSource a)
        {
            a.View?.Refresh();
        }

        if (AppLogService.Logs.Count > 0)
        {
            MainListView.ScrollIntoView(AppLogService.Logs[^1]);
        }
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshView();
    }

    private void ButtonScrollToBottom_OnClick(object sender, RoutedEventArgs e)
    {
        MainListView.ScrollIntoView(AppLogService.Logs[^1]);
    }

    private void ButtonCopySelectedLogs_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var logs = MainListView.SelectedItems.OfType<object>().Select(i => i.ToString()!).ToList();
            Clipboard.SetDataObject(string.Join('\n', logs));
        }
        catch (Exception ex)
        {
            App.GetService<ILogger<AppLogsWindow>>().LogError(ex, "无法复制日志到剪切板。");
        }
    }

    private void ButtonClearLogs_OnClick(object sender, RoutedEventArgs e)
    {
        AppLogService.Logs.Clear();
    }
}