using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Models;
using ClassIsland.Models.Actions;
using ClassIsland.Shared;
using Microsoft.Extensions.DependencyInjection;
namespace ClassIsland.Services.Automation.Actions;

[ActionInfo("classisland.settings", "应用设置", "\uef27", addDefaultToMenu: false)]
public class SettingsAction : ActionBase<SettingsActionSettings>
{
    SettingsService SettingsService { get; } = IAppHost.Host.Services.GetService<SettingsService>();

    protected override async Task OnInvoke()
    {
        if (IsRevertable)
            SettingsService.AddSettingsOverlay(ActionSet.Guid, Settings.Name, Settings.Value);
        else
        {
            var info = typeof(Settings).GetProperty(Settings.Name, SettingsService.SettingsPropertiesFlags);
            if (info == null)
                throw new KeyNotFoundException($"应用设置中找不到属性“{Settings.Name}”。");

            var type = info.PropertyType;
            var value = Convert.ChangeType(Settings.Value, type);
            info.SetValue(SettingsService.Settings, value);
        }
    }

    protected override async Task OnRevert()
    {
        SettingsService.RemoveSettingsOverlay(ActionSet.Guid, Settings.Name);
    }
}