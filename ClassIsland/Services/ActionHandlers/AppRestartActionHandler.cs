using System;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.Hosting;
namespace ClassIsland.Services.ActionHandlers;

public class AppRestartActionHandler : IHostedService
{
    public AppRestartActionHandler(SettingsService SettingsService, IActionService ActionService)
    {
        // ActionService.RegisterActionHandler("classisland.app.restart",
        //     (s, g) => App.Current.Restart((s as AppRestartActionSettings).Value));
    }

    public async Task StartAsync(CancellationToken _) { }
    public async Task StopAsync(CancellationToken _) { }
}
