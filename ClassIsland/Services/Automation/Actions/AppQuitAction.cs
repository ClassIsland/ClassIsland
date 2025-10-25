using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
namespace ClassIsland.Services.Automation.Actions;

[ActionInfo("classisland.app.quit", "退出 ClassIsland", "\ue0df", addDefaultToMenu:false)]
public class AppQuitAction : ActionBase
{
    protected override async Task OnInvoke()
    {
        AppBase.Current.Stop();
    }
}