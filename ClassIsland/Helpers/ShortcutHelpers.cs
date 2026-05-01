using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform;
using ClassIsland.Core;
using ClassIsland.Platforms.Abstraction;
using Mono.Unix;
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

            await CreateFreedesktopShortcutAsync(desktopPath, false);
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

            await CreateFreedesktopShortcutAsync(startMenuPath, false);
            await CopyFreeDesktopIconAsync();
        }
    }
    
    public static async Task CreateClassSwapShortcutAsync(string path="")
    {
        var desktopPath = string.IsNullOrEmpty(path) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "快捷换课.url") : path;
        var stream = AssetLoader.Open(new Uri("avares://ClassIsland/Assets/ShortcutTemplates/ClassSwap.url"));

        await File.WriteAllTextAsync(desktopPath, await new StreamReader(stream).ReadToEndAsync());
    }

    public static async Task CreateFreedesktopShortcutAsync(string path = "", bool isAutostart = false)
    {
        if (AppBase.Current.PackagingType == "deb")
        {
            File.Copy("/usr/share/applications/cn.classisland.app.desktop", path, true);
            SetUnixExecutePermission(path);
            return;
        }
        var targetPath = string.IsNullOrEmpty(path)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local/share/applications/cn.classisland.app.desktop") : path;

        var raw = await new StreamReader(
                AssetLoader.Open(new Uri("avares://ClassIsland/Assets/ShortcutTemplates/cn.classisland.app.desktop")))
            .ReadToEndAsync();
        var args = isAutostart ? "--autostartup" : "--uri %u";
        var final = string.Format(raw, AppBase.AppVersion, AppBase.ExecutingEntrance, args);
        await File.WriteAllTextAsync(targetPath, final);
        SetUnixExecutePermission(path);
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

    public static void SetUnixExecutePermission(string path)
    {
        if (!OperatingSystem.IsLinux()) 
            return;
        var unixFileInfo = new UnixFileInfo(path);
        unixFileInfo.FileAccessPermissions |= FileAccessPermissions.UserExecute | FileAccessPermissions.GroupExecute |
                                              FileAccessPermissions.OtherExecute;
    }

    /// <summary>
    /// 检查并更新自启动快捷方式，确保包含 --autostartup 参数
    /// </summary>
    public static async Task CheckAndUpdateAutostartShortcutAsync()
    {
        var shortcutInfo = GetAutostartShortcutInfo();
        if (shortcutInfo == null)
            return;

        if (!File.Exists(shortcutInfo.Path))
            return;

        var needsUpdate = await CheckShortcutNeedsUpdateAsync(shortcutInfo);
        if (!needsUpdate)
            return;

        RefreshAutostartSettings();
    }

    private static AutostartShortcutInfo? GetAutostartShortcutInfo()
    {
        if (OperatingSystem.IsWindows())
        {
            return new AutostartShortcutInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ClassIsland.lnk"),
                AutostartShortcutType.Windows);
        }
        else if (OperatingSystem.IsLinux())
        {
            return new AutostartShortcutInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/autostart/cn.classisland.app.desktop"),
                AutostartShortcutType.Linux);
        }
        return null;
    }

    private static async Task<bool> CheckShortcutNeedsUpdateAsync(AutostartShortcutInfo shortcutInfo)
    {
        try
        {
            if (shortcutInfo.Type == AutostartShortcutType.Windows)
            {
                using var shortcut = WindowsShortcut.Load(shortcutInfo.Path);
                return string.IsNullOrWhiteSpace(shortcut.Arguments) || !shortcut.Arguments.Contains("--autostartup");
            }
            else if (shortcutInfo.Type == AutostartShortcutType.Linux)
            {
                var content = await File.ReadAllTextAsync(shortcutInfo.Path);
                return !content.Contains("Exec=") || !content.Contains("--autostartup");
            }
            return false;
        }
        catch
        {
            return true; // 如果读取失败，假设需要更新
        }
    }

    private static void RefreshAutostartSettings()
    {
        try
        {
            var isEnabled = PlatformServices.DesktopService.IsAutoStartEnabled;
            if (isEnabled)
            {
                PlatformServices.DesktopService.IsAutoStartEnabled = false;
                PlatformServices.DesktopService.IsAutoStartEnabled = true;
            }
        }
        catch
        {
            // 忽略错误
        }
    }

    private class AutostartShortcutInfo
    {
        public string Path { get; }
        public AutostartShortcutType Type { get; }

        public AutostartShortcutInfo(string path, AutostartShortcutType type)
        {
            Path = path;
            Type = type;
        }
    }

    private enum AutostartShortcutType
    {
        Windows,
        Linux
    }
}