using System.Diagnostics;
using System.Linq;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using Material.Icons;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.lifetime.startup", "应用启动时", MaterialIconKind.AutoStart)]
public class AppStartupTrigger : TriggerBase
{
    public override void Loaded()
    {
        var stack = new StackTrace();
        if (stack.GetFrames().FirstOrDefault(x => x.GetMethod()?.DeclaringType == typeof(App)) != null)
        {
            Trigger();
        }
    }

    public override void UnLoaded()
    {
    }
}