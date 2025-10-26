using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform;
using ClassIsland.Core;
using WindowsShortcutFactory;

namespace ClassIsland.Helpers;

public static class ShortcutHelpers
{
    public static async Task CreateDesktopShortcutAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            var desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "ClassIsland.lnk");

            using var shortcut = new WindowsShortcut();
            shortcut.Path = AppBase.ExecutingEntrance;
            shortcut.WorkingDirectory = Path.GetDirectoryName(AppBase.ExecutingEntrance);
            shortcut.Save(desktopPath);
        }
        else if (OperatingSystem.IsLinux())
        {
            var desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "ClassIsland.desktop");

            await CreateFreedesktopShortcutAsync(desktopPath);
            await CopyFreeDesktopIconAsync();
        }
    }

    public static async Task CreateStartMenuShortcutAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            var startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "ClassIsland.lnk");

            using var shortcut = new WindowsShortcut();
            shortcut.Path = AppBase.ExecutingEntrance;
            shortcut.WorkingDirectory = Path.GetDirectoryName(AppBase.ExecutingEntrance);
            shortcut.Save(startMenuPath);
        }
        else if (OperatingSystem.IsLinux())
        {
            var startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local/share/applications/cn.classisland.app.desktop");

            await CreateFreedesktopShortcutAsync(startMenuPath);
            await CopyFreeDesktopIconAsync();
        }
    }
    
    public static async Task CreateClassSwapShortcutAsync(string path="")
    {
        var desktopPath = string.IsNullOrEmpty(path) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "快捷换课.url") : path;
        var stream = AssetLoader.Open(new Uri("avares://ClassIsland/Assets/ShortcutTemplates/ClassSwap.url"));

        await File.WriteAllTextAsync(desktopPath, await new StreamReader(stream).ReadToEndAsync());
    }

    public static async Task CreateFreedesktopShortcutAsync(string path = "")
    {
        if (AppBase.Current.PackagingType == "deb")
        {
            File.Copy("/usr/share/applications/cn.classisland.app.desktop", path, true);
            return;
        }
        var targetPath = string.IsNullOrEmpty(path)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local/share/applications/cn.classisland.app.desktop") : path;

        var raw = await new StreamReader(
                AssetLoader.Open(new Uri("avares://ClassIsland/Assets/ShortcutTemplates/cn.classisland.app.desktop")))
            .ReadToEndAsync();
        var final = string.Format(raw, AppBase.AppVersion, AppBase.ExecutingEntrance);
        await File.WriteAllTextAsync(targetPath, final);
    }

    public static async Task CopyFreeDesktopIconAsync()
    {
        if (AppBase.Current.PackagingType is "folder" or "folderClassic") // 仅在绿色版下才需要手动复制图标，安装版应当由安装程序将图标复制到系统目录。)
        {
            var iconsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "icons/hicolor/128x128/apps/");
            if (!Directory.Exists(iconsDir))
            {
                Directory.CreateDirectory(iconsDir);
            }

            await using var src = AssetLoader.Open(new Uri("avares://ClassIsland/Assets/FreedesktopIcons/AppLogo@128w.png"));
            await using var file = File.OpenWrite(Path.Combine(iconsDir, "classisland.png"));
            await src.CopyToAsync(file);
        }
    }
}