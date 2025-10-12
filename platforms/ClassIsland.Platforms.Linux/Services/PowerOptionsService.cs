using System.Diagnostics;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Linux.Services;

/// <summary>
/// Linux平台的电源操作实现，需要systemd作为前提
/// </summary>
public class PowerOptionsService:IPowerOptionsService
{
    public void Shutdown()
    {
        Process.Start("systemctl", "poweroff");
    }

    public void Reboot()
    {
        Process.Start("systemctl", "reboot");
    }

    public void Logout()
    {
        throw new PlatformNotSupportedException();
    }
    public void Hibernate()
    {
        Process.Start("systemctl", "hibernate");
    }

    public void Sleep()
    {
        Process.Start("systemctl", "suspend");
    }
}