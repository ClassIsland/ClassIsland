using System;
using System.IO;
using System.Windows;
using Avalonia.Platform;
using Pastel;

namespace ClassIsland.Services;

public class ConsoleService
{
    public static string AsciiLogo = "";

    public static void InitializeConsole()
    {
        PrintAppInfo();
    }

    public static void PrintAppInfo()
    {
        var s = AssetLoader.Open(new Uri("avares://ClassIsland/Assets/AsciiLogo.txt"));
        AsciiLogo = new StreamReader(s).ReadToEnd();
        Console.WriteLine(AsciiLogo.Pastel("#00bfff"));
        Console.WriteLine($"ClassIsland {App.AppVersionLong}");
        Console.WriteLine("「赐你，众星俱焚的曙光！」".Pastel("#F4EF74"));
        Console.WriteLine();
    }

    public ConsoleService()
    {
    }
}