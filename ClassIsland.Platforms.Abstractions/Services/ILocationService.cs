using ClassIsland.Platforms.Abstraction.Models;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 位置获取服务
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// 获取设备当前的地理位置
    /// </summary>
    /// <returns>地理位置坐标</returns>
    public Task<LocationCoordinate> GetLocationAsync();
}