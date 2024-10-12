using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.Actions;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Hosting;
using System.Threading;
namespace ClassIsland.Services.ActionHandlers;

public class SleepActionHandler : IHostedService
{
    public SleepActionHandler(IActionService ActionService)
    {
        ActionService.RegisterActionHandler("classisland.action.sleep",
            (s, g) => {
                Task.Delay(TimeSpan.FromSeconds(((SleepActionSettings)s!).Value))
                    .Wait();
            });
    }

    public async Task StartAsync(CancellationToken _) { }
    public async Task StopAsync(CancellationToken _) { }
}
