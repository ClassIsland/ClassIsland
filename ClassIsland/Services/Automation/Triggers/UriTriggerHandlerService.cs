using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.EventArgs;
using System;

namespace ClassIsland.Services.Automation.Triggers;

public class UriTriggerHandlerService
{
    private IUriNavigationService UriNavigationService { get; }

    internal event EventHandler<UriTriggerHandledEventArgs>? HandledRun;
    internal event EventHandler<UriTriggerHandledEventArgs>? HandledRevert;

    public UriTriggerHandlerService(IUriNavigationService uriNavigationService)
    {
        UriNavigationService = uriNavigationService;

        UriNavigationService.HandleAppNavigation("api/automation/run", args =>
        {
            HandledRun?.Invoke(this, new UriTriggerHandledEventArgs(string.Join('/', args.ChildrenPathPatterns)));
        });
        UriNavigationService.HandleAppNavigation("api/automation/revert", args =>
        {
            HandledRevert?.Invoke(this, new UriTriggerHandledEventArgs(string.Join('/', args.ChildrenPathPatterns)));
        });
    }
}