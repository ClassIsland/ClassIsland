using System;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Controls.MiniInfoProvider;
using ClassIsland.Core.Interfaces;
using ClassIsland.Models;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services.MiniInfoProviders;

public class CountDownMiniInfoProvider : IMiniInfoProvider, IHostedService
{
    public string Name { get; set; } = "考试倒计时";
    public string Description { get; set; } = "显示距离某考试剩余的天数";
    public Guid ProviderGuid { get; set; } = new Guid("DE09B49D-FE61-11EE-9DF4-43208C458CC8");
    public object? SettingsElement { get; set; }
    public object InfoElement { get; set; }

    private SettingsService SettingsService { get; }

    private Settings Settings => SettingsService.Settings;

    private CountDownMiniInfoProviderSettings CountDownMiniInfoProviderSettings { get; set; }

    public CountDownMiniInfoProvider(SettingsService settingsService, MiniInfoProviderHostService miniInfoProviderHostService)
    {
        SettingsService = settingsService;
        miniInfoProviderHostService.RegisterMiniInfoProvider(this);
        CountDownMiniInfoProviderSettings =
            miniInfoProviderHostService.GetMiniInfoProviderSettings<CountDownMiniInfoProviderSettings>(ProviderGuid)
            ?? new();
        InfoElement = new CountDownMiniInfoProviderControl(CountDownMiniInfoProviderSettings);
        SettingsElement = new CountDownMiniInfoProviderSettingsControl(CountDownMiniInfoProviderSettings);
        miniInfoProviderHostService.WriteMiniInfoProviderSettings(ProviderGuid, CountDownMiniInfoProviderSettings);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        
    }
}
