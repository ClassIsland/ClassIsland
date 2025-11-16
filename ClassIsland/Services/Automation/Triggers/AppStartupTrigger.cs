using System.Diagnostics;
using System.Linq;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.lifetime.startup", "应用启动时", "\ue067")]
public class AppStartupTrigger : TriggerBase
{
    public override void Loaded()
    {
        if (AppBase.CurrentLifetime < ApplicationLifetime.Running) {
            Trigger();
        }
    }

    public override void UnLoaded()
    {
    }
}