using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using ClassIsland.Core;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Logging;
using ClassIsland.Services.Logging;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using DynamicData;
using DynamicData.Binding;
using FluentAvalonia.UI.Data;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using WebSocketSharp;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace ClassIsland.Views;

/// <summary>
/// AppLogsWindow.xaml 的交互逻辑
/// </summary>
public partial class AppLogsWindow : MyWindow
{
    public static readonly FuncValueConverter<LogLevel, string> LogLevelToIconGlyphConverter = new(x => x switch
    {
        LogLevel.Trace => "\uE50F",
        LogLevel.Debug => "\uE2C7",
        LogLevel.Information => "\uE9E4",
        LogLevel.Warning => "\uF430",
        LogLevel.Error => "\uE808",
        LogLevel.Critical => "\uE84E",
        LogLevel.None => "\uEDF6;",
        _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
    });
    
    public static readonly FuncValueConverter<LogLevel, string> LogLevelToNameConverter = new(x => x switch
    {
        LogLevel.Trace => "追踪",
        LogLevel.Debug => "调试",
        LogLevel.Information => "信息",
        LogLevel.Warning => "警告",
        LogLevel.Error => "错误",
        LogLevel.Critical => "灾难",
        LogLevel.None => "？？？",
        _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
    });
    
    public AppLogsViewModel ViewModel { get; } = IAppHost.GetService<AppLogsViewModel>();

    private bool _isOpened = false;

    public AppLogsWindow()
    {
        InitializeComponent();
        DataContext = this;

        // ViewModel.AppLogService.Log.Subscribe(_ =>
            // DataGridMain.ScrollIntoView(ViewModel.Logs[^1], DataGridMain.Columns.Last()));
        // ViewModel.ObservableForProperty(x => x.Logs)
        //     .Subscribe(_ => );
        RefreshView();
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

    private void AppLogsWindow_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            return;
        }
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

    private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshView();
    }

    private void RefreshView()
    {
        
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshView();
    }


    private void ButtonCopySelectedLogs_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var logs = DataGridMain.SelectedItems.Cast<object?>().Select(x => x?.ToString() ?? "").ToList();
            Clipboard?.SetTextAsync(string.Join(Environment.NewLine, logs));
            this.ShowSuccessToast($"已将 {logs.Count} 条日志复制到剪贴板。");
        }
        catch (Exception ex)
        {
            App.GetService<ILogger<AppLogsWindow>>().LogError(ex, "无法复制日志到剪切板。");
            this.ShowErrorToast("无法复制日志到剪切板。", ex);
        }
    }

    private void ButtonClearLogs_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.AppLogService.Logs.Clear();
    }
}

