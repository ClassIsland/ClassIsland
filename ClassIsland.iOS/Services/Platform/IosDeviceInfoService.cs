using ClassIsland.Platforms.Abstraction.Services;
using UIKit;

namespace ClassIsland.iOS.Services.Platform;

internal sealed class IosDeviceInfoService : IDeviceInfoService
{
    private readonly string _model = "???";

    public IosDeviceInfoService()
    {
        try
        {
            // 在平台入口的主线程读取 UIKit，诊断导出时可从后台线程访问缓存。
            _model = UIDevice.CurrentDevice.Model;
        }
        catch
        {
            // 设备信息仅用于诊断，查询失败时不影响应用运行。
        }
    }

    public void GetDeviceInfo(out string name, out string vendor)
    {
        name = _model;
        vendor = "Apple Inc.";
    }
}
