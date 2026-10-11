using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using AndroidX.Core.Content;
using AndroidX.Core.Location;
using AndroidX.Core.Util;
using Avalonia.Threading;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Android.Services.Platform;

public class LocationService : ILocationService
{
    private readonly Context _context = Application.Instance;
    private Task<LocationCoordinate>? _locationTask;

    public async Task<LocationCoordinate> GetLocationAsync()
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_locationTask is not { IsCompleted: false })
            {
                _locationTask = GetLocationCoreAsync();
            }

            return _locationTask;
        });
    }

    private async Task<LocationCoordinate> GetLocationCoreAsync()
    {
        try
        {
            if (!HasLocationPermission())
            {
                if (MainActivity.Current?.TryGetTarget(out var activity) != true ||
                    activity == null || !activity.IsForeground || activity.IsFinishing || activity.IsDestroyed)
                {
                    throw new UnauthorizedAccessException("请打开 ClassIsland 主界面并允许访问位置，然后重试。");
                }

                if (!await activity.RequestLocationPermissionAsync() || !HasLocationPermission())
                {
                    throw new UnauthorizedAccessException("未获得位置权限，请在系统设置中允许 ClassIsland 访问位置，然后重试。");
                }
            }

            using var manager = _context.GetSystemService(Context.LocationService) as LocationManager;
            if (manager == null || !LocationManagerCompat.IsLocationEnabled(manager))
            {
                throw new InvalidOperationException("定位服务未开启或不可用，请在系统设置中开启定位，然后重试。");
            }

            var enabledProviders = manager.GetProviders(true);
            var providers = new List<string>();
            if (enabledProviders.Contains(LocationManager.NetworkProvider))
            {
                providers.Add(LocationManager.NetworkProvider);
            }

            if (_context.CheckSelfPermission(global::Android.Manifest.Permission.AccessFineLocation) == Permission.Granted &&
                enabledProviders.Contains(LocationManager.GpsProvider))
            {
                providers.Add(LocationManager.GpsProvider);
            }

            if (providers.Count == 0)
            {
                throw new InvalidOperationException("没有可用的定位方式，请检查系统定位设置或允许精确位置，然后重试。");
            }

            var completion = new TaskCompletionSource<LocationCoordinate>(TaskCreationOptions.RunContinuationsAsynchronously);
            var remainingProviders = providers.Count;
            using var executor = ContextCompat.GetMainExecutor(_context);
            var cancellations = new List<CancellationSignal>();
            var consumers = new List<LocationConsumer>();
            try
            {
                foreach (var provider in providers)
                {
                    var consumer = new LocationConsumer(location =>
                    {
                        if (location != null &&
                            double.IsFinite(location.Longitude) && double.IsFinite(location.Latitude) &&
                            location.Longitude is >= -180 and <= 180 && location.Latitude is >= -90 and <= 90)
                        {
                            completion.TrySetResult(new LocationCoordinate
                            {
                                Longitude = location.Longitude,
                                Latitude = location.Latitude
                            });
                        }

                        if (--remainingProviders == 0)
                        {
                            completion.TrySetException(new InvalidOperationException("无法获取当前位置，请检查定位设置并移至信号良好的位置，然后重试。"));
                        }
                    });
                    consumers.Add(consumer);
                    // Each provider needs its own signal because Android stores only one cancellation listener.
                    var cancellation = new CancellationSignal();
                    cancellations.Add(cancellation);
                    LocationManagerCompat.GetCurrentLocation(manager, provider, cancellation, executor, consumer);
                }

                return await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch (TimeoutException exception)
            {
                throw new TimeoutException("获取位置超时，请移至信号良好的位置，然后重试。", exception);
            }
            finally
            {
                completion.TrySetCanceled();
                foreach (var cancellation in cancellations)
                {
                    cancellation.Cancel();
                    cancellation.Dispose();
                }
                foreach (var consumer in consumers)
                {
                    consumer.Dispose();
                }
            }
        }
        catch (Java.Lang.SecurityException exception)
        {
            throw new UnauthorizedAccessException("无法访问位置，请打开 ClassIsland 主界面并在系统设置中允许访问位置，然后重试。", exception);
        }
    }

    private bool HasLocationPermission()
    {
        return _context.CheckSelfPermission(global::Android.Manifest.Permission.AccessCoarseLocation) == Permission.Granted ||
               _context.CheckSelfPermission(global::Android.Manifest.Permission.AccessFineLocation) == Permission.Granted;
    }

    private sealed class LocationConsumer(Action<Location?> callback) : Java.Lang.Object, IConsumer
    {
        public void Accept(Java.Lang.Object? value)
        {
            callback(value as Location);
        }
    }
}
