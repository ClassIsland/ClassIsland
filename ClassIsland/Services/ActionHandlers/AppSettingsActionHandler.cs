using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.Actions;
using System.Threading.Tasks;

using System.Threading;
using Microsoft.Extensions.Hosting;
namespace ClassIsland.Services.ActionHandlers;

internal class AppSettingsActionHandler
{
    internal AppSettingsActionHandler(SettingsService SettingsService, IActionService ActionService)
    {
        var RegisterActionHandler = ActionService.RegisterActionHandler;
        var RegisterActionBackHandler = ActionService.RegisterActionBackHandler;
        var AddSettingsOverlay = SettingsService.AddSettingsOverlay;
        var RemoveSettingsOverlay = SettingsService.RemoveSettingsOverlay;

        RegisterActionHandler("classisland.settings.currentComponentConfig",
            (s, g) => AddSettingsOverlay(g, "CurrentComponentConfig", ((CurrentComponentConfigActionSettings) s!).Value));
        RegisterActionHandler("classisland.settings.theme",
            (s, g) => AddSettingsOverlay(g, "Theme", ((ThemeActionSettings) s!).Value));
        RegisterActionHandler("classisland.settings.windowDockingLocation",
            (s, g) => AddSettingsOverlay(g, "WindowDockingLocation", ((WindowDockingLocationActionSettings) s!).Value));

        RegisterActionBackHandler("classisland.settings.currentComponentConfig",
            (s, g) => RemoveSettingsOverlay(g, "CurrentComponentConfig"));
        RegisterActionBackHandler("classisland.settings.theme",
            (s, g) => RemoveSettingsOverlay(g, "Theme"));
        RegisterActionBackHandler("classisland.settings.windowDockingLocation",
            (s, g) => RemoveSettingsOverlay(g, "WindowDockingLocation"));
    }
}
