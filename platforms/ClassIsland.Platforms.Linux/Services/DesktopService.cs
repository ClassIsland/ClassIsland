using ClassIsland.Helpers;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Linux.Services;

public class DesktopService : IDesktopService
{
    public bool IsAutoStartEnabled
    {
        get =>
            File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config/autostart/cn.classisland.app.desktop"));
        set
        {
            var startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config/autostart/cn.classisland.app.desktop");
            if (value)
            {
                _ = ShortcutHelpers.CreateFreedesktopShortcutAsync(startupPath);
            } else if (File.Exists(startupPath))
            {
                File.Delete(startupPath);
            }
        }
    }

    public bool IsUrlSchemeRegistered
    {
        get => false;set{} 
    }
}