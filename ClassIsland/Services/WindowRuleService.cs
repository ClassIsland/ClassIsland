using System;
using Windows.Win32.UI.Accessibility;
using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Core;
using ClassIsland.Models.Rules;
using System.Windows.Forms;

namespace ClassIsland.Services;

public class WindowRuleService : IWindowRuleService
{
    public ILogger<WindowRuleService> Logger { get; }
    public IRulesetService RulesetService { get; }

    public event WINEVENTPROC? ForegroundWindowChanged;
    public HWND ForegroundHwnd { get; set; }

    private List<HWINEVENTHOOK> _hooks = new();

    private bool _isMoving = false;

    public WindowRuleService(ILogger<WindowRuleService> logger, IRulesetService rulesetService)
    {
        Logger = logger;
        RulesetService = rulesetService;
        eventProc = PfnWinEventProc;
        uint[] events = [EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_MOVESIZEEND, EVENT_SYSTEM_MOVESIZESTART,
            EVENT_SYSTEM_MINIMIZEEND, EVENT_OBJECT_LOCATIONCHANGE];
        foreach (var i in events)
        {
            var flags = WINEVENT_OUTOFCONTEXT;
            if (i == EVENT_OBJECT_LOCATIONCHANGE)
            {
                flags |= WINEVENT_SKIPOWNPROCESS;
            }
            var hook = SetWinEventHook(
                i, i,
                HMODULE.Null, eventProc,
                0, 0,
                flags);
            _hooks.Add(hook);
        }
        ForegroundWindowChanged += ((_, _, _, _, _, _, _) => RulesetService.NotifyStatusChanged());

        RulesetService.RegisterRuleHandler("classisland.windows.className", ClassNameHandler);
        RulesetService.RegisterRuleHandler("classisland.windows.text", TextHandler);
        RulesetService.RegisterRuleHandler("classisland.windows.status", StatusHandler);
        RulesetService.RegisterRuleHandler("classisland.windows.processName", ProcessNameHandler);
    }

    private unsafe bool ProcessNameHandler(object? settings)
    {
        if (settings is not StringMatchingSettings s) return false;
        uint pid = 0;
        GetWindowThreadProcessId(ForegroundHwnd, &pid);
        var process = Process.GetProcessById((int)pid);
        return s.IsMatching(process.ProcessName);
    }

    private bool StatusHandler(object? settings)
    {
        if (settings is not WindowStatusRuleSettings s) return false;
        GetWindowRect(ForegroundHwnd, out var rect);
        var mw = App.GetService<MainWindow>();
        var screen = mw.ViewModel.Settings.WindowDockingMonitorIndex < Screen.AllScreens.Length &&
                     mw.ViewModel.Settings.WindowDockingMonitorIndex >= 0 ?
            Screen.AllScreens[mw.ViewModel.Settings.WindowDockingMonitorIndex] : Screen.PrimaryScreen;
        if (screen == null)
        {
            return false;
        }

        var fullscreen = NativeWindowHelper.IsForegroundFullScreen(screen);
        var maximize = IsZoomed(ForegroundHwnd);
        var minimize = IsIconic(ForegroundHwnd);
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
        using var className = new DisposablePWSTR(256);
        GetWindowText(ForegroundHwnd, className.PWSTR, 256);
        return s.IsMatching(className.ToString());
    }

    private bool ClassNameHandler(object? settings)
    {
        if (settings is not StringMatchingSettings s) return false;
        using var className = new DisposablePWSTR(256);
        GetClassName(ForegroundHwnd, className.PWSTR, 256);
        return s.IsMatching(className.ToString());
    }

    ~WindowRuleService()
    {
        foreach (var i in _hooks)
        {
            UnhookWinEvent(i);
        }
    }

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private static WINEVENTPROC eventProc;
    private void PfnWinEventProc(HWINEVENTHOOK hook, uint @event, HWND hwnd, int idObject, int child, uint thread, uint time)
    {
        if (hwnd != GetForegroundWindow())
        {
            return;
        }

        if (@event == EVENT_OBJECT_LOCATIONCHANGE && _isMoving)
        {
            return;
        }

        _isMoving = @event switch
        {
            EVENT_SYSTEM_MOVESIZESTART => true,
            EVENT_SYSTEM_MOVESIZEEND => false,
            _ => _isMoving
        };

        ForegroundHwnd = GetForegroundWindow();
        //Logger.LogTrace("Window event: {} HWND:{} {}", @event, hwnd, hook.Value);
        _ = Dispatcher.CurrentDispatcher.InvokeAsync(() =>
        {
            ForegroundWindowChanged?.Invoke(hook, @event, hwnd, idObject, child, thread, time);
        });
    }

    public unsafe bool IsForegroundWindowClassIsland()
    {
        uint pid = 0;
        GetWindowThreadProcessId(ForegroundHwnd, &pid);
        var process = Process.GetProcessById((int)pid);
        return process.Id == Environment.ProcessId;
    }
}