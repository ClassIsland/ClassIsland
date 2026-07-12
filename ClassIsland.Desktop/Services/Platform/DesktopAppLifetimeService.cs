using System.Diagnostics;
using ClassIsland.Core;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Desktop.Services.Platform;

public class DesktopAppLifetimeService : IAppLifetimeService
{
    public void Shutdown()
    {
        AppBase.Current.DesktopLifetime?.Shutdown();
    }

    public void Restart(string[] parameters, bool restartToLauncher)
    {
        var path = Environment.ProcessPath;
        if (path == null)
            return;
        var replaced = path.Replace(".dll", AppBase.PlatformExecutableExtension);
        var startInfo = new ProcessStartInfo(restartToLauncher ? AppBase.ExecutingEntrance : replaced);
        foreach (var i in parameters)
        {
            startInfo.ArgumentList.Add(i);
        }
        Process.Start(startInfo);
    }
}