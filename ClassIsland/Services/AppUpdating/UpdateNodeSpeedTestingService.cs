using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.AppUpdating;

public class UpdateNodeSpeedTestingService
{
    private SettingsService SettingsService { get; }

    private ILogger<UpdateNodeSpeedTestingService> Logger { get; }

    public UpdateNodeSpeedTestingService(SettingsService settingsService, ILogger<UpdateNodeSpeedTestingService> logger)
    {
        SettingsService = settingsService;
        Logger = logger;
    }

    public async Task RunSpeedTestAsync()
    {
        var updateService = IAppHost.TryGetService<UpdateService>();
        if (updateService == null)
        {
            return;
        }
        foreach (var i in SettingsService.Settings.SpeedTestResults)
        {
            i.Value.IsTesting = true;
        }
        var client = new HttpClient()
        {

        };

        foreach (var i in updateService.Index.Mirrors)
        {
            var delays = new List<long>();
            foreach (var k in i.Value.SpeedTestUrls)
            {
                var ping = new Ping();
                Logger.LogInformation("Https prepare get {}", k);
                try
                {
                    var canConnect = false;
                    var r = await client.GetAsync(new Uri($"https://{k}", UriKind.RelativeOrAbsolute));
                    //Logger.LogInformation("Https get {}, {}ms", k, sw.ElapsedMilliseconds);
                    i.Value.SpeedTestResult.CanConnect = true;
                    await Task.Run(() =>
                    {
                        var pr = ping.Send(k, 4096);
                        if (pr.Status != IPStatus.Success)
                        {
                            i.Value.SpeedTestResult.IsDelayUnclear = true;
                            i.Value.SpeedTestResult.Delay = 9999;
                            return;
                        }
                        Logger.LogInformation("Ping {}, {}ms", k, pr.RoundtripTime);
                        delays.Add(pr.RoundtripTime);

                    });
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Unable to ping {}", k);
                    i.Value.SpeedTestResult.CanConnect = false;
                    break;
                }
            }

            if (i.Value.SpeedTestResult.CanConnect && delays.Count > 0)
            {
                var sum = delays.Sum();
                i.Value.SpeedTestResult.Delay = sum / delays.Count;
            }

            i.Value.SpeedTestResult.IsTested = true;
            i.Value.SpeedTestResult.IsTesting = false;
        }
        Logger.LogInformation("Ping completed.");
        SettingsService.Settings.LastSpeedTest = DateTime.Now;

        if (SettingsService.Settings.IsAutoSelectUpgradeMirror)
        {
            if (updateService.Index.Mirrors.TryGetValue("main", out var mainMirror) &&
                mainMirror.SpeedTestResult.CanConnect)
            {
                SettingsService.Settings.SelectedUpdateMirrorV2 = "main";
                return;
            }
            var re = from i in updateService.Index.Mirrors
                     where i.Value.SpeedTestResult.CanConnect
                     orderby i.Value.SpeedTestResult.Delay
                     select i.Key;
            foreach (var i in re)
            {
                SettingsService.Settings.SelectedUpdateMirrorV2 = i;
                break;
            }
        }
    }
}