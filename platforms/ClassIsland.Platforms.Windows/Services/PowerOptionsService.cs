using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.System.Shutdown;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platform.Windows.Services;

/// <summary>
/// Windows平台的电源操作实现，使用Win32 API
/// </summary>
[SupportedOSPlatform("windows5.1.2600")]
public class PowerOptionsService:IPowerOptionsService
{
    public void Shutdown()=>ExitWindowsEx(EXIT_WINDOWS_FLAGS.EWX_SHUTDOWN, SHUTDOWN_REASON.SHTDN_REASON_FLAG_PLANNED);

    public void Reboot()=>ExitWindowsEx(EXIT_WINDOWS_FLAGS.EWX_REBOOT,SHUTDOWN_REASON.SHTDN_REASON_FLAG_PLANNED);

    public void Hibernate()=>SetSuspendState(true,false,false);

    public void Sleep()=>SetSuspendState(false, false, false);

}