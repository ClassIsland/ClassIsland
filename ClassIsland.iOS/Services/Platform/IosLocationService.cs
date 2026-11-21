using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;
using CoreLocation;
using Foundation;
using UIKit;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 使用 iOS Core Location 获取一次设备当前位置。
/// </summary>
internal sealed class IosLocationService : ILocationService
{
    private static readonly TimeSpan LocationTimeout = TimeSpan.FromSeconds(10);
    private readonly TaskCompletionSource<bool> _applicationReady =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// 标记 Avalonia 根视图已经显示，可以安全弹出系统授权对话框。
    /// </summary>
    public void SetApplicationReady()
    {
        _applicationReady.TrySetResult(true);
    }

    public async Task<LocationCoordinate> GetLocationAsync()
    {
        await _applicationReady.Task.ConfigureAwait(false);
        var request = new LocationRequest();
        UIApplication.SharedApplication.BeginInvokeOnMainThread(request.Start);

        try
        {
            var completedTask = await Task.WhenAny(
                    request.Completion,
                    Task.Delay(LocationTimeout))
                .ConfigureAwait(false);

            if (completedTask != request.Completion)
            {
                request.Fail(new TimeoutException("获取当前位置超时，请检查定位服务是否可用后重试。"));
            }

            return await request.Completion.ConfigureAwait(false);
        }
        finally
        {
            await request.StopAsync().ConfigureAwait(false);
        }
    }

    private sealed class LocationRequest : CLLocationManagerDelegate
    {
        private readonly TaskCompletionSource<LocationCoordinate> _completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private CLLocationManager? _manager;
        private int _completed;

        public Task<LocationCoordinate> Completion => _completionSource.Task;

        public void Start()
        {
            if (_completionSource.Task.IsCompleted)
            {
                return;
            }

            try
            {
                if (!CLLocationManager.LocationServicesEnabled)
                {
                    Fail(new InvalidOperationException("系统定位服务未开启，请在系统设置中开启定位服务后重试。"));
                    return;
                }

                _manager = new CLLocationManager
                {
                    Delegate = this
                };

                switch (_manager.AuthorizationStatus)
                {
                    case CLAuthorizationStatus.AuthorizedAlways:
                    case CLAuthorizationStatus.AuthorizedWhenInUse:
                        _manager.StartUpdatingLocation();
                        break;
                    case CLAuthorizationStatus.NotDetermined:
                        _manager.RequestWhenInUseAuthorization();
                        break;
                    case CLAuthorizationStatus.Denied:
                    case CLAuthorizationStatus.Restricted:
                    default:
                        Fail(new InvalidOperationException("ClassIsland 没有定位权限，请在系统设置中允许使用定位。"));
                        break;
                }
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        public void Fail(Exception exception)
        {
            if (Interlocked.Exchange(ref _completed, 1) != 0)
            {
                return;
            }

            _completionSource.TrySetException(exception);
        }

        public Task StopAsync()
        {
            var completionSource = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (_manager is { } manager)
                    {
                        manager.StopUpdatingLocation();
                        manager.Delegate = null!;
                        manager.Dispose();
                        _manager = null;
                    }
                }
                finally
                {
                    completionSource.TrySetResult(true);
                }
            });

            return completionSource.Task;
        }

        public override void AuthorizationChanged(
            CLLocationManager manager,
            CLAuthorizationStatus status)
        {
            switch (status)
            {
                case CLAuthorizationStatus.AuthorizedAlways:
                case CLAuthorizationStatus.AuthorizedWhenInUse:
                    manager.StartUpdatingLocation();
                    break;
                case CLAuthorizationStatus.Denied:
                case CLAuthorizationStatus.Restricted:
                    Fail(new InvalidOperationException("ClassIsland 没有定位权限，请在系统设置中允许使用定位。"));
                    break;
            }
        }

        public override void LocationsUpdated(
            CLLocationManager manager,
            CLLocation[] locations)
        {
            foreach (var location in locations)
            {
                var coordinate = location.Coordinate;
                if (location.HorizontalAccuracy < 0 ||
                    double.IsNaN(coordinate.Latitude) ||
                    double.IsNaN(coordinate.Longitude) ||
                    double.IsInfinity(coordinate.Latitude) ||
                    double.IsInfinity(coordinate.Longitude) ||
                    coordinate.Latitude is < -90 or > 90 ||
                    coordinate.Longitude is < -180 or > 180)
                {
                    continue;
                }

                if (Interlocked.Exchange(ref _completed, 1) != 0)
                {
                    return;
                }

                _completionSource.TrySetResult(new LocationCoordinate
                {
                    Latitude = coordinate.Latitude,
                    Longitude = coordinate.Longitude
                });
                return;
            }
        }

        public override void Failed(CLLocationManager manager, NSError error)
        {
            if (error.Code == (nint)CLError.Denied)
            {
                Fail(new InvalidOperationException("ClassIsland 没有定位权限，请在系统设置中允许使用定位。"));
                return;
            }

            Fail(new InvalidOperationException(
                $"无法获取当前位置：{error.LocalizedDescription ?? "定位服务返回未知错误。"}"));
        }
    }
}
