using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Linux.Services;

/// <summary>
/// Linux 设备信息服务。
/// </summary>
public class DeviceInfoService : IDeviceInfoService
{
    /// <inheritdoc />
    public void GetDeviceInfo(out string name, out string vendor)
    {
        name = "???";
        vendor = "???";
        try
        {
            if (File.Exists("/sys/devices/virtual/dmi/id/product_name"))
                name = File.ReadAllText("/sys/devices/virtual/dmi/id/product_name").Trim();
            if (File.Exists("/sys/devices/virtual/dmi/id/sys_vendor"))
                vendor = File.ReadAllText("/sys/devices/virtual/dmi/id/sys_vendor").Trim();
        }
        catch
        {
            // 设备信息仅用于诊断，查询失败时不影响应用运行。
        }
    }
}
