using System.Management;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platform.Windows.Services;

/// <summary>
/// Windows 设备信息服务。
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
            using var managementClass = new ManagementClass("Win32_ComputerSystemProduct");
            using var instances = managementClass.GetInstances();
            foreach (var instance in instances)
            {
                using (instance)
                {
                    name = instance.GetPropertyValue("Name") as string ?? "???";
                    vendor = instance.GetPropertyValue("Vendor") as string ?? "???";
                }
            }
        }
        catch
        {
            // 设备信息仅用于诊断，查询失败时不影响应用运行。
        }
    }
}
