using System;
using System.IO;
using System.Windows;

using Pastel;

namespace ClassIsland.Services;

public class ConsoleService
{
    public static string AsciiLogo = "";
    public static HWND ConsoleHWnd { get; private set; }

    public static void InitializeConsole()
    {
#if DEBUG
        if (ConsoleHWnd == nint.Zero)
        {
            AllocConsole();
        }
        ConsoleHWnd = GetConsoleWindow();
        SetWindowText(ConsoleHWnd, "ClassIsland 输出");
#endif
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
        Console.WriteLine("「火萤这种生物很神奇吧？它们或许会扑向火烛，或许会突然老化。但在那之前的每一个夜晚，它们的光芒会比星星更耀眼。」".Pastel("#A0D9B4"));
        Console.WriteLine();
    }

    public ConsoleService()
    {
        
    }
}