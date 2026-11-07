using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <summary>
/// 设备信息服务桩。
/// </summary>
public class DeviceInfoServiceStub : IDeviceInfoService
{
    internal DeviceInfoServiceStub()
    {
    }

    /// <inheritdoc />
    public void GetDeviceInfo(out string name, out string vendor)
    {
        name = "???";
        vendor = "???";
    }
}
