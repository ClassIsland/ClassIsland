using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.Actions;
using System.Threading.Tasks;
using System;
namespace ClassIsland.Services.ActionHandlers;

internal class SleepActionHandler
{
    internal SleepActionHandler(IActionService ActionService)
    {
        ActionService.RegisterActionHandler("classisland.action.sleep",
            (s, g) => {
                Task.Delay(TimeSpan.FromSeconds(((SleepActionSettings)s!).Value))
                    .Wait();
            });
    }
}
