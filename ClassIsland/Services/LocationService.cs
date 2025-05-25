using System;
using System.Device.Location;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models;

namespace ClassIsland.Services;

public class LocationService : ILocationService
{

    public async Task<LocationCoordinate> GetLocationAsync()
    {
        using var watcher = new GeoCoordinateWatcher();
        try
        {
            var cancel = new CancellationTokenSource();
            cancel.CancelAfter(TimeSpan.FromSeconds(10));
            await Task.Run(() =>
            {
                watcher.TryStart(false, TimeSpan.FromSeconds(10));
                while (watcher.Status != GeoPositionStatus.Ready && !cancel.IsCancellationRequested)
                {
                    
                }
            }, cancel.Token);
            var coord = watcher.Position.Location;
            var locationCoordinate = new LocationCoordinate()
            {
                Longitude = coord.Longitude,
                Latitude = coord.Latitude
            };
            if (double.IsNaN(locationCoordinate.Latitude) || double.IsNaN(locationCoordinate.Longitude))
            {
                throw new InvalidOperationException("获取的位置信息无效，可能是定位服务未开启或系统不支持");
            }
            return locationCoordinate;
        }
        finally
        {
            watcher.Stop();
        }
    }
}