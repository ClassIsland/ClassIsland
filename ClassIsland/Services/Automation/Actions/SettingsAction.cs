using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
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
    static SettingsService SettingsService { get; } = IAppHost.Host.Services.GetService<SettingsService>();

    protected override async Task OnInvoke()
    {
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

                var type = info.PropertyType;
                var value = Deserialize(Settings.Value, type);
                info.SetValue(SettingsService.Settings, value);
            }
        });
    }

    protected override async Task OnRevert()
    {
        await base.OnRevert();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SettingsService.RemoveSettingsOverlay(ActionSet.Guid, Settings.Name);
        });
    }

    public static readonly JsonSerializerOptions FriendlyJsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static readonly HashSet<Type> EasyTypes =
        [typeof(string), typeof(int), typeof(double), typeof(bool), typeof(Color)];


    public static string SerializeByName(string name, out PropertyInfo info)
    {
        info = SettingsService.GetPropertyInfoByName(name);
        return Serialize(info.GetValue(SettingsService.Settings), info.PropertyType);
    }

    public static object DeserializeByName(string value, string name, out PropertyInfo info)
    {
        info = SettingsService.GetPropertyInfoByName(name);
        return Deserialize(value, info.PropertyType);
    }

    public static string Serialize(object value, Type type) => EasyTypes.Contains(type)
        ? value.ToString()
        : JsonSerializer.Serialize(value, FriendlyJsonSerializerOptions);

    public static object Deserialize(string value, Type type) => EasyTypes.Contains(type)
        ? type == typeof(Color) ? Color.Parse(value) : Convert.ChangeType(value, type)
        : JsonSerializer.Deserialize(value, type);
}