using System;
using static ClassIsland.NativeWindowHelper;

namespace ClassIsland.Services;

public class ConsoleService
{
    private static bool _consoleVisible;
    public static nint ConsoleHWnd { get; private set; }

    public static bool ConsoleVisible
    {
        get => _consoleVisible;
        set
        {
            if (value)
            {
                ShowWindow(ConsoleHWnd, SW_SHOW);
            }
            else
            {
                ShowWindow(ConsoleHWnd, SW_HIDE);
            }
            _consoleVisible = value;
        }
    }

    public static void InitializeConsole()
    {
        if (ConsoleHWnd == nint.Zero)
        {
            AllocConsole();
        }
        ConsoleHWnd = GetConsoleWindow();
        SetWindowText(ConsoleHWnd, "ClassIsland输出");
        ShowWindow(ConsoleHWnd, SW_HIDE);
    }

    public ConsoleService()
    {
        
    }
}