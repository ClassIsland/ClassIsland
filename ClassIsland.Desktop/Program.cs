#if Platforms_Windows
using ClassIsland.Platform.Windows.Services;
#endif
using ClassIsland.Platforms.Abstraction;

namespace ClassIsland.Desktop;

class Program
{
    [STAThread]
    static async Task<int> Main(string[] args)
    {
        ActivatePlatforms();
        return await ClassIsland.Program.AppEntry(args);
    }

    static void ActivatePlatforms()
    {
#if Platforms_Windows
        PlatformServices.WindowPlatformService = new WindowPlatformService();
        PlatformServices.LocationService = new LocationService();
#endif
    }
}