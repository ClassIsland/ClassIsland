using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.Automation.Triggers;
using ClassIsland.Models.EventArgs;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.uri", "调用 Uri 时", PackIconKind.Link)]
public class UriTrigger(UriTriggerHandlerService uriTriggerHandlerService) : TriggerBase<UriTriggerSettings>
{
    private UriTriggerHandlerService UriTriggerHandlerService { get; } = uriTriggerHandlerService;

    public override void Loaded()
    {
        UriTriggerHandlerService.HandledRun += UriTriggerHandlerServiceOnHandledRun;
        UriTriggerHandlerService.HandledRevert += UriTriggerHandlerServiceOnHandledRevert;
    }

    private void UriTriggerHandlerServiceOnHandledRevert(object? sender, UriTriggerHandledEventArgs e)
    {
        if (e.Name == Settings.UriSuffix)
        {
            TriggerRevert();
        }
    }

    private void UriTriggerHandlerServiceOnHandledRun(object? sender, UriTriggerHandledEventArgs e)
    {
        if (e.Name == Settings.UriSuffix)
        {
            Trigger();
        }
    }

    public override void UnLoaded()
    {
        UriTriggerHandlerService.HandledRun -= UriTriggerHandlerServiceOnHandledRun;
        UriTriggerHandlerService.HandledRevert -= UriTriggerHandlerServiceOnHandledRevert;
    }
}