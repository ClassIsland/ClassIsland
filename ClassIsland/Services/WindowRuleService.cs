using System;
using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Core;
using ClassIsland.Models.Rules;
using Avalonia.Threading;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Models;

namespace ClassIsland.Services;

public class WindowRuleService : IWindowRuleService
{
    public ILogger<WindowRuleService> Logger { get; }
    public IRulesetService RulesetService { get; }

    public event EventHandler<ForegroundWindowChangedEventArgs>? ForegroundWindowChanged;


    private bool _isMoving = false;

    public WindowRuleService(ILogger<WindowRuleService> logger, IRulesetService rulesetService)
    {
        Logger = logger;
        RulesetService = rulesetService;
        
        ForegroundWindowChanged += ((_, _) => RulesetService.NotifyStatusChanged());
        PlatformServices.WindowPlatformService.RegisterForegroundWindowChangedEvent((_, e) => ForegroundWindowChanged?.Invoke(this, e));

        RulesetService.RegisterRuleHandler("classisland.windows.className", ClassNameHandler);
        RulesetService.RegisterRuleHandler("classisland.windows.text", TextHandler);
        RulesetService.RegisterRuleHandler("classisland.windows.status", StatusHandler);
        RulesetService.RegisterRuleHandler("classisland.windows.processName", ProcessNameHandler);
    }

    private unsafe bool ProcessNameHandler(object? settings)
    {
        if (settings is not StringMatchingSettings s) return false;
        var pid = PlatformServices.WindowPlatformService.GetWindowPid(PlatformServices.WindowPlatformService
            .ForegroundWindowHandle);
        if (pid == 0)
        {
            return false;
        }
        try
        {
            var process = Process.GetProcessById((int)pid);
            return s.IsMatching(process.ProcessName);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "无法获取 pid 为 {} 的进程信息", pid);
            return false;
        }
    }

    private bool StatusHandler(object? settings)
    {
        if (settings is not WindowStatusRuleSettings s) return false;
        var mw = App.GetService<MainWindow>();
        var screen = mw.GetSelectedScreenSafe();
        if (screen == null)
        {
            return false;
        }

        var fullscreen = PlatformServices.WindowPlatformService.IsForegroundWindowFullscreen(screen);
        var maximize = PlatformServices.WindowPlatformService.IsWindowMaximized(PlatformServices.WindowPlatformService
            .ForegroundWindowHandle);
        var minimize = PlatformServices.WindowPlatformService.IsWindowMinimized(PlatformServices.WindowPlatformService
            .ForegroundWindowHandle);
        return s.State switch
        {
            0 => !(fullscreen || maximize || minimize),
            1 => maximize && !fullscreen,
            2 => minimize,
            3 => fullscreen,
            _ => false
        };
    }

    private bool TextHandler(object? settings)
    {
        if (settings is not StringMatchingSettings s) return false;
        return s.IsMatching(PlatformServices.WindowPlatformService.GetWindowTitle(PlatformServices.WindowPlatformService
            .ForegroundWindowHandle));
    }

    private bool ClassNameHandler(object? settings)
    {
        if (settings is not StringMatchingSettings s) return false;
        return s.IsMatching(PlatformServices.WindowPlatformService.GetWindowClassName(PlatformServices.WindowPlatformService
            .ForegroundWindowHandle));
    }


    public unsafe bool IsForegroundWindowClassIsland()
    {
        var pid = PlatformServices.WindowPlatformService.GetWindowPid(PlatformServices.WindowPlatformService
            .ForegroundWindowHandle);
        var process = Process.GetProcessById(pid);
        return process.Id == Environment.ProcessId;
    }
}