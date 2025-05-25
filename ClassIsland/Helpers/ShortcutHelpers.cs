using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ClassIsland.Helpers;

public static class ShortcutHelpers
{
    public static async Task CreateClassSwapShortcutAsync(string path="")
    {
        var desktopPath = string.IsNullOrEmpty(path) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "快捷换课.url") : path;
        var stream = Application.GetResourceStream(new Uri("/Assets/ShortcutTemplates/ClassSwap.url", UriKind.Relative));
        if (stream == null)
        {
            return;
        }

        await File.WriteAllTextAsync(desktopPath, await new StreamReader(stream.Stream).ReadToEndAsync());
    }
}