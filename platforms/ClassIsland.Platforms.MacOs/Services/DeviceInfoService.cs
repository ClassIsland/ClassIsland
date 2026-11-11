using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.MacOs.Services;

/// <summary>
/// macOS 设备信息服务。
/// </summary>
public class DeviceInfoService : IDeviceInfoService
{
    /// <inheritdoc />
    public void GetDeviceInfo(out string name, out string vendor)
    {
        name = "Macintosh";
        vendor = "Apple Inc.";
    }
}
