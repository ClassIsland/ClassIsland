namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 平台设备信息服务。
/// </summary>
public interface IDeviceInfoService
{
    /// <summary>
    /// 获取设备信息，查询失败时保留未知值，不抛出异常。
    /// </summary>
    /// <param name="name">设备型号，未知时为“???”。</param>
    /// <param name="vendor">设备厂商，未知时为“???”。</param>
    void GetDeviceInfo(out string name, out string vendor);
}
