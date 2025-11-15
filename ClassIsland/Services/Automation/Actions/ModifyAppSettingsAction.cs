using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Models;
using ClassIsland.Models.Actions;
namespace ClassIsland.Services.Automation.Actions;

[ActionInfo("classisland.settings", "应用设置", "\uef27", addDefaultToMenu: false)]
public class ModifyAppSettingsAction : ActionBase<ModifyAppSettingsActionSettings>
{
    SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    protected override async Task OnInvoke()
    {
        if (Settings.Name == "") return;
        await base.OnInvoke();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (IsRevertable)
                SettingsService.AddSettingsOverlay(ActionSet.Guid, Settings.Name, Settings.Value);
            else
            {
                var info = typeof(Settings).GetProperty(Settings.Name, SettingsService.SettingsPropertiesFlags);
                if (info == null)
                    throw new KeyNotFoundException($"应用设置中找不到属性“{Settings.Name}”。");
                info?.SetValue(SettingsService.Settings, Settings.Value);
            }
        });
    }

    protected override async Task OnRevert()
    {
        if (Settings.Name == "") return;
        await base.OnRevert();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SettingsService.RemoveSettingsOverlay(ActionSet.Guid, Settings.Name);
        });
    }

    public static readonly JsonSerializerOptions FriendlyJsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    [Pure]
    public static Type GetUnderlyingType(Type propertyType) =>
        Nullable.GetUnderlyingType(propertyType) ?? propertyType;

    public static readonly HashSet<Type> EasyTypes =
        [typeof(string), typeof(int), typeof(double), typeof(bool)];
}