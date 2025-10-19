using System.Diagnostics;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.MacOs.Services;

/// <summary>
/// macOS平台的电源操作实现，使用Apple Script
/// </summary>
public class PowerOptionsService:IPowerOptionsService
{
    public void Shutdown()
    {
        Process.Start("osascript","-e 'tell app \"System Events\" to shut down'");
    }

    public void Reboot()
    {
        Process.Start("osascript","-e 'tell app \"System Events\" to restart'");
    }
    
    public void Hibernate()
    {
        Process.Start("osascript","-e 'tell application \"System Events\" to sleep'");
    }

    public void Sleep()
    {
        Process.Start("osascript","-e 'tell application \"System Events\" to sleep'");
    }
}