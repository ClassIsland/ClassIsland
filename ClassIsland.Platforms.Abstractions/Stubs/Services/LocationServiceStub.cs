using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <summary>
/// 位置服务桩
/// </summary>
public class LocationServiceStub : ILocationService
{
    internal LocationServiceStub()
    {
        
    }
    
    public async Task<LocationCoordinate> GetLocationAsync()
    {
        return new LocationCoordinate()
        {
            Longitude = 0,
            Latitude = 0
        };
    }
}