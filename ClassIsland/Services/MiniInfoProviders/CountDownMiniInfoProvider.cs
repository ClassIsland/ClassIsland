using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ClassIsland.Controls.Components;
using ClassIsland.Controls.MiniInfoProvider;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Models;

using Google.Protobuf.WellKnownTypes;

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
        CountDownMiniInfoProviderSettings.DaysLeft = (CountDownMiniInfoProviderSettings.overTime - DateTime.Today).Days;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}
