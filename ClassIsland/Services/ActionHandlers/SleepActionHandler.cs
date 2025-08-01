using ClassIsland.Core.Abstractions.Services;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Hosting;
using System.Threading;
using ClassIsland.Helpers;
namespace ClassIsland.Services.ActionHandlers;

public class SleepActionHandler : IHostedService
{
    public SleepActionHandler(IActionService ActionService)
    {
        // ActionService.RegisterActionHandler("classisland.action.sleep",
        //     (s, g) => {
        //         Task.Delay(TimeSpanHelper.FromSecondsSafe(((SleepActionSettings)s!).Value))
        //             .Wait();
        //     });
    }

    public async Task StartAsync(CancellationToken _) { }
    public async Task StopAsync(CancellationToken _) { }
}
