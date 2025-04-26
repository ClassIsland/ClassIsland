﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.Actions;
using Microsoft.Extensions.Hosting;
namespace ClassIsland.Services.ActionHandlers;

public class AppSettingsActionHandler : IHostedService
{
    public AppSettingsActionHandler(SettingsService SettingsService, IActionService ActionService)
    {
        var Reg = ActionService.RegisterActionHandler;
        var RegRevert = ActionService.RegisterRevertHandler;

        Reg("classisland.settings.currentComponentConfig", (s, g) =>
            Add(g, "CurrentComponentConfig", ((CurrentComponentConfigActionSettings)s!).Value));
        Reg("classisland.settings.theme", (s, g) => 
            Add(g, "Theme", ((ThemeActionSettings)s!).Value));
        Reg("classisland.settings.windowDockingLocation", (s, g) =>
            Add(g, "WindowDockingLocation", ((WindowDockingLocationActionSettings)s!).Value));
        Reg("classisland.settings.windowLayer", (s, g) =>
            Add(g, "WindowLayer", ((WindowLayerActionSettings)s!).Value));
        Reg("classisland.settings.windowDockingOffsetX", (s, g) =>
            Add(g, "WindowDockingOffsetX", ((WindowDockingOffsetXActionSettings)s!).Value));
        Reg("classisland.settings.windowDockingOffsetY", (s, g) =>
            Add(g, "WindowDockingOffsetY", ((WindowDockingOffsetYActionSettings)s!).Value));

        RegRevert("classisland.settings.currentComponentConfig", (s, g) => 
            Remove(g, "CurrentComponentConfig"));
        RegRevert("classisland.settings.theme", (s, g) => 
            Remove(g, "Theme"));
        RegRevert("classisland.settings.windowDockingLocation", (s, g) => 
            Remove(g, "WindowDockingLocation"));
        RegRevert("classisland.settings.windowLayer", (s, g) =>
            Remove(g, "WindowLayer"));
        RegRevert("classisland.settings.windowDockingOffsetX", (s, g) =>
            Remove(g, "WindowDockingOffsetX"));
        RegRevert("classisland.settings.windowDockingOffsetY", (s, g) =>
            Remove(g, "WindowDockingOffsetY"));

        void Add(string g, string binding, dynamic value)
        {
            App.Current.Dispatcher.Invoke(
                new Action(() => SettingsService.AddSettingsOverlay(g, binding, value)),
                DispatcherPriority.Render);
        }

        void Remove(string g, string binding)
        {
            App.Current.Dispatcher.Invoke(
                new Action(() => SettingsService.RemoveSettingsOverlay(g, binding)),
                DispatcherPriority.Render);
        }
    }

    public async Task StartAsync(CancellationToken _) { }
    public async Task StopAsync(CancellationToken _) { }
}
