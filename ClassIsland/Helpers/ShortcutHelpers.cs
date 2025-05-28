using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Platform;

namespace ClassIsland.Helpers;

public static class ShortcutHelpers
{
    public static async Task CreateClassSwapShortcutAsync(string path="")
    {
        var desktopPath = string.IsNullOrEmpty(path) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "快捷换课.url") : path;
        var stream = AssetLoader.Open(new Uri("/Assets/ShortcutTemplates/ClassSwap.url", UriKind.Relative));

        await File.WriteAllTextAsync(desktopPath, await new StreamReader(stream).ReadToEndAsync());
    }
}