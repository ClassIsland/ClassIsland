using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Platform;
using ClassIsland.Core;

namespace ClassIsland.Helpers;

public static class ShortcutHelpers
{
    public static async Task CreateClassSwapShortcutAsync(string path="")
    {
        var desktopPath = string.IsNullOrEmpty(path) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "快捷换课.url") : path;
        var stream = AssetLoader.Open(new Uri("/Assets/ShortcutTemplates/ClassSwap.url", UriKind.Relative));

        await File.WriteAllTextAsync(desktopPath, await new StreamReader(stream).ReadToEndAsync());
    }

    public static async Task CreateFreedesktopShortcutAsync(string path = "")
    {
        var targetPath = string.IsNullOrEmpty(path)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local/share/applications/cn.classisland.app.desktop") : path;

        var raw = await new StreamReader(
                AssetLoader.Open(new Uri("avares://ClassIsland/Assets/ShortcutTemplates/cn.classisland.app.desktop")))
            .ReadToEndAsync();
        var final = string.Format(raw, AppBase.AppVersion, AppBase.ExecutingEntrance);
        await File.WriteAllTextAsync(targetPath, final);
    }
}