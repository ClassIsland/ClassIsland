using System.Diagnostics;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Desktop.Services.Platform;

public class LauncherService : ILauncherService
{
    public async Task LaunchPath(string path)
    {
        var topLevel = AppBase.Current.PhonyRootWindow;
        if (PlatformServices.FilePickerService.IsBookmark(path))
        {
            var file = await PlatformServices.FilePickerService.GetFileAsync(path, topLevel) as IStorageItem
                       ?? await PlatformServices.FilePickerService.GetFolderAsync(path, topLevel);
            if (file != null)
            {
                await topLevel.Launcher.LaunchFileAsync(file);
                return;
            }
        }

        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(path),
            UseShellExecute = true
        });
    }

    public async Task LaunchUrl(string url)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}