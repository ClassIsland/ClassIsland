using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ClassIsland.Helpers;

public class ShortcutHelpers
{
    public static async Task CreateClassSwapShortcutAsync()
    {
        var desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "快捷换课.url");
        var stream = Application.GetResourceStream(new Uri("/Assets/ShortcutTemplates/ClassSwap.url", UriKind.Relative));
        if (stream == null)
        {
            return;
        }

        await File.WriteAllTextAsync(desktopPath, new StreamReader(stream.Stream).ReadToEnd());
    }
}