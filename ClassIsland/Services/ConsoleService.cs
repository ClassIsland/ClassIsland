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
        Console.WriteLine("「钟表的指针周而复始，就像人的困惑、烦恼、软弱…摇摆不停。但最终，人们依旧要前进，就像你的指针，永远落在前方。」".Pastel("#48C0F8"));
        Console.WriteLine();
    }

    public ConsoleService()
    {
    }
}