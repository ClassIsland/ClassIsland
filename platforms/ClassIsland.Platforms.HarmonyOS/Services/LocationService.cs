using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platform.HarmonyOS.Services;

/// <summary>
/// HarmonyOS 位置服务实现
/// </summary>
public class LocationService : ILocationService
{
    public async Task<LocationCoordinate> GetLocationAsync()
    {
        // TODO: 实现HarmonyOS位置获取逻辑
        // HarmonyOS可能需要通过位置服务API来获取地理位置
        // 需要申请位置权限并调用相应的系统API
        
        await Task.Delay(100); // 模拟异步操作
        
        // 返回默认位置（北京）作为占位符
        return new LocationCoordinate
        {
            Latitude = 39.9042,
            Longitude = 116.4074
        };
    }
}
