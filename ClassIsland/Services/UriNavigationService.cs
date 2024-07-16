using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Core.Models.UriNavigation;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ClassIsland.Services;

public class UriNavigationService(ILogger<UriNavigationService> logger) : IUriNavigationService
{
    private ILogger<UriNavigationService> Logger { get; } = logger;

    private UriNavigationNode NavigationHandlers { get; } = new("")
    {
        NavigatedAction = _ => throw new KeyNotFoundException("找不到要导航的目标。")
    };

    public void HandleNavigation(string domain, string path, Action<UriNavigationEventArgs> onNavigated)
    {
        var uri = new UriBuilder(IUriNavigationService.UriScheme, domain)
        {
            Path = path
        };
        Logger.LogDebug("注册uri处理器：{}", uri);
        if (NavigationHandlers.Contains(uri.ToString()))
        {
            throw new ArgumentException($"给定的uri {uri} 已经被注册。");
        }

        NavigationHandlers.AddNode(domain + uri.Uri.AbsolutePath, onNavigated);
    }

    public void HandleAppNavigation(string path, Action<UriNavigationEventArgs> onNavigated)
    {
        HandleNavigation(IUriNavigationService.UriDomainApp, path, onNavigated);
    }

    public void HandlePluginsNavigation(string path, Action<UriNavigationEventArgs> onNavigated)
    {
        HandleNavigation(IUriNavigationService.UriDomainPlugins, path, onNavigated);
    }

    public void Navigate(Uri uri)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (uri.Scheme == IUriNavigationService.UriScheme)
            {
                Logger.LogInformation("正在 Uri 导航：{}", uri);
                var node = NavigationHandlers.GetNode(uri.Host + uri.AbsolutePath, out var children);
                node.NavigatedAction?.Invoke(new UriNavigationEventArgs(uri, children));
            }
            else
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = uri.ToString(),
                    UseShellExecute = true
                });
            }
        });
    }

    public void NavigateWrapped(Uri uri, out Exception? exception)
    {
        Exception? exc = null;
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                Navigate(uri);
            }
            catch (Exception ex)
            {
                exc = ex;
                Logger.LogError(ex, "无法导航到 {}", uri);
                CommonDialog.ShowError($"无法导航到 {uri}：{ex.Message}");
            }
        });
        exception = exc;
    }

    public void NavigateWrapped(Uri uri)
    {
        NavigateWrapped(uri, out var _);
    }
}