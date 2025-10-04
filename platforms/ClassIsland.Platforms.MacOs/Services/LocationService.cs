using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;
using CoreLocation;

namespace ClassIsland.Platforms.MacOs.Services;

public class LocationService : ILocationService
{
    public LocationService()
    {
    }
    
    public async Task<LocationCoordinate> GetLocationAsync()
    {
        using var manager = new CLLocationManager();
        manager.RequestWhenInUseAuthorization();
        var tokenSource = new CancellationTokenSource();
        tokenSource.CancelAfter(TimeSpan.FromSeconds(10.0));
        var location = manager.Location ?? new CLLocation();
        return new LocationCoordinate()
        {
            Longitude = location.Coordinate.Longitude,
            Latitude = location.Coordinate.Latitude
        };
    }
}