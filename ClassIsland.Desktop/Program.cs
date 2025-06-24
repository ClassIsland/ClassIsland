using ClassIsland.Platforms.Abstraction;

namespace ClassIsland.Desktop;

class Program
{
    [STAThread]
    static async Task<int> Main(string[] args)
    {
        return await ClassIsland.Program.AppEntry(args);
    }

    static void ActivatePlatforms()
    {
        
#if Platforms_Windows
#endif
    }
}