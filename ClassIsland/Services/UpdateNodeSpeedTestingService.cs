using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

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
        foreach (var i in SettingsService.Settings.SpeedTestResults)
        {
            i.Value.IsTesting = true;
        }
        var client = new HttpClient()
        {
            
        };
        foreach (var i in UpdateService.UpdateSources)
        {
            var delays = new List<long>();
            foreach (var k in i.Value.SpeedTestSources)
            {
                var ping = new Ping();
                Logger.LogInformation("Https prepare get {}", k);
                try
                {
                    var canConnect = false;
                    await Task.Run(() =>
                    {
                        var pr = ping.Send(k, 2048);
                        if (pr.Status != IPStatus.Success)
                            return;
                        Logger.LogInformation("Ping {}, {}ms", k, pr.RoundtripTime);
                        delays.Add(pr.RoundtripTime);
                        canConnect = true;
                    });
                    if (!canConnect)
                    {
                        i.Value.SpeedTestResult.CanConnect = false;
                        break;
                    }
                    var r = await client.GetAsync(new Uri($"https://{k}", UriKind.RelativeOrAbsolute));
                    //Logger.LogInformation("Https get {}, {}ms", k, sw.ElapsedMilliseconds);
                    i.Value.SpeedTestResult.CanConnect = true;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Unable to ping {}", k);
                    i.Value.SpeedTestResult.CanConnect = false;
                    break;
                }
            }

            if (i.Value.SpeedTestResult.CanConnect)
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
            var re = from i in UpdateService.UpdateSources
                where i.Value.SpeedTestResult.CanConnect
                orderby i.Value.SpeedTestResult.Delay
                select i.Key;
            foreach (var i in re)
            {
                SettingsService.Settings.SelectedUpgradeMirror = i;
                break;
            }
        }
    }
}