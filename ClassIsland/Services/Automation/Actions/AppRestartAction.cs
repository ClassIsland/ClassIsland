using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.Actions;
namespace ClassIsland.Services.Automation.Actions;

[ActionInfo("classisland.app.restart", "重启 ClassIsland", "\ue0bd", addDefaultToMenu:false)]
public class AppRestartAction : ActionBase<AppRestartActionSettings>
{
    protected override async Task OnInvoke()
    {
        AppBase.Current.Restart(Settings.Value);
    }
}