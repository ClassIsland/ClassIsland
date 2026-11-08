using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Android.Services.Platform;

/// <summary>
/// Android 设备信息服务。
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
            vendor = global::Android.OS.Build.Manufacturer ?? "???";
            name = global::Android.OS.Build.Model ?? "???";
        }
        catch
        {
            // 设备信息仅用于诊断，查询失败时不影响应用运行。
        }
    }
}
