using System;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Controls.MiniInfoProvider;
using ClassIsland.Interfaces;
using ClassIsland.Models;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services.MiniInfoProviders;

public class WeatherMiniInfoProvider : IMiniInfoProvider, IHostedService
{
    public string Name { get; set; } = "天气";
    public string Description { get; set; } = "显示当前的天气信息。";
    public Guid ProviderGuid { get; set; } = new Guid("EA336289-5A60-49EF-AD36-858109F37644");
    public object? SettingsElement { get; set; }
    public object InfoElement { get; set; }

    private SettingsService SettingsService { get; }

    private Settings Settings => SettingsService.Settings;

    public WeatherMiniInfoProvider(SettingsService settingsService, MiniInfoProviderHostService miniInfoProviderHostService)
    {
        SettingsService = settingsService;
        InfoElement = new WeatherMiniInfoProviderControl(SettingsService);
        miniInfoProviderHostService.RegisterMiniInfoProvider(this);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}