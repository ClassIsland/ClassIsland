using System.Diagnostics;
using System.Linq;
using ClassIsland.Core.Abstractions.Services;
using Microsoft.Win32;

namespace ClassIsland.Helpers;

public static class UriProtocolRegisterHelper
{
    public static void Register()
    {
        var root = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Classes", RegistryKeyPermissionCheck.ReadWriteSubTree)?.CreateSubKey(IUriNavigationService.UriScheme);
        var shellKey = root!.CreateSubKey("shell");
        var openKey = shellKey.CreateSubKey("open");
        var commandKey = openKey.CreateSubKey("command");
        root.SetValue("URL Protocol", "");
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        commandKey.SetValue("", $"\"{exePath}\" --uri \"%1\"");
    }

    public static void UnRegister()
    {
        Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Classes", RegistryKeyPermissionCheck.ReadWriteSubTree)?.DeleteSubKeyTree(IUriNavigationService.UriScheme);
    }

    public static bool IsRegistered()
    {
        return Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Classes")?.GetSubKeyNames().Count(x => x == IUriNavigationService.UriScheme) > 0;
    }
}