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
        manager.StartUpdatingLocation();
        try
        {
            var tokenSource = new CancellationTokenSource();
            var location = new CLLocation();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(10.0));
            manager.UpdatedLocation += (sender, args) =>
            {
                location = args.NewLocation;
                tokenSource.Cancel();
            };
            await Task.Run(() => tokenSource.Token.WaitHandle.WaitOne(), tokenSource.Token);
            return new LocationCoordinate()
            {
                Longitude = location.Coordinate.Longitude,
                Latitude = location.Coordinate.Latitude
            };
        }
        finally
        {
            manager.StopUpdatingLocation();
        }
    }
}