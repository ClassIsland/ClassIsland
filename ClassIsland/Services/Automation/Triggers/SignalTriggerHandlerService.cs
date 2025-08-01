using System;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.Automation.Triggers;
using ClassIsland.Models.EventArgs;

namespace ClassIsland.Services.Automation.Triggers;

public class SignalTriggerHandlerService
{
    public event EventHandler<SignalTriggerEventArgs>? Handled;

    public void EmitSignal(string name, bool revert)
    {
        Handled?.Invoke(this, new SignalTriggerEventArgs(name, revert));
    }

    public SignalTriggerHandlerService(IActionService actionService)
    {
        // actionService.RegisterActionHandler("classisland.broadcastSignal", (o, guid) =>
        // {
        //     if (o is SignalTriggerSettings settings)
        //     {
        //         EmitSignal(settings.SignalName, settings.IsRevert);
        //     }
        // });
        // actionService.RegisterRevertHandler("classisland.broadcastSignal", (o, guid) =>
        // {
        //     if (o is SignalTriggerSettings settings)
        //     {
        //         EmitSignal(settings.SignalName, !settings.IsRevert);
        //     }
        // });
    }
}