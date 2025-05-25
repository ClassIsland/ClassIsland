using System;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Controls.MiniInfoProvider;
using ClassIsland.Shared.Interfaces;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services.MiniInfoProviders;

public class DateMiniInfoProvider : IMiniInfoProvider, IHostedService
{
    public string Name { get; set; } = "日期";
    public string Description { get; set; } = "显示今天的日期和星期。";
    public Guid ProviderGuid { get; set; } = new Guid("D9FC55D6-8061-4C21-B521-6B0532FF735F");
    public object? SettingsElement { get; set; }
    public object InfoElement { get; set; }

    private MiniInfoProviderHostService MiniInfoProviderHostService { get; }

    public DateMiniInfoProvider(MiniInfoProviderHostService miniInfoProviderHostService)
    {
        MiniInfoProviderHostService = miniInfoProviderHostService;
        MiniInfoProviderHostService.RegisterMiniInfoProvider(this);
        InfoElement = new DateMiniInfoProviderControl();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}