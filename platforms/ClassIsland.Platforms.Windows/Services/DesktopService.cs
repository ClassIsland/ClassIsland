using ClassIsland.Helpers;
using ClassIsland.Platforms.Abstraction.Services;
using WindowsShortcutFactory;

namespace ClassIsland.Platform.Windows.Services;

public class DesktopService : IDesktopService
{
    public bool IsAutoStartEnabled
    {
        get => File.Exists(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ClassIsland.lnk"));
        set
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ClassIsland.lnk");
            if (value)
            {
                using var shortcut = new WindowsShortcut();
                shortcut.Path = Environment.ProcessPath;
                shortcut.WorkingDirectory = Environment.CurrentDirectory;
                shortcut.Save(path);
            }
            else
            {
                File.Delete(path);
            }
            
        }
    }
    public bool IsUrlSchemeRegistered{
        get => UriProtocolRegisterHelper.IsRegistered();
        set
        {
            if (value)
            {
                UriProtocolRegisterHelper.Register();
            }
            else
            {
                UriProtocolRegisterHelper.UnRegister();
            }
        }
    }
}