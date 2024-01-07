using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Pastel;
using static ClassIsland.NativeWindowHelper;
using Color = System.Drawing.Color;

namespace ClassIsland.Services;

public class ConsoleService
{
    private static bool _consoleVisible;

    public static string AsciiLogo = "";
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
        SetWindowText(ConsoleHWnd, "ClassIsland 输出");
        ShowWindow(ConsoleHWnd, SW_HIDE);
        PrintAppInfo();
    }

    public static void PrintAppInfo()
    {
        var s = Application.GetResourceStream(new Uri("/Assets/AsciiLogo.txt", UriKind.RelativeOrAbsolute))?.Stream;
        if (s != null)
        {
            AsciiLogo = new StreamReader(s).ReadToEnd();
        }
        Console.WriteLine(AsciiLogo.Pastel("#00bfff"));
        Console.WriteLine($"ClassIsland {App.AppVersionLong}");
        Console.WriteLine("星星划过的时候，要记得许愿哦".Pastel("#e29cd7"));
        Console.WriteLine();
    }

    public ConsoleService()
    {
        
    }
}