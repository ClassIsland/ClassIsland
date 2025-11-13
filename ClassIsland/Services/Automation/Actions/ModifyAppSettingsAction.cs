using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
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

    [Pure] public static bool IsTypeSupported(Type type)
    {
        if (type == null) return false;

        type = GetUnderlyingType(type);

        if (EasyTypes.Contains(type) || type == typeof(Color))
            return true;

        return type.IsEnum;
    }

    [Pure]
    public static Type GetUnderlyingType(Type propertyType)
    {
        Type? s = null;
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            s = Nullable.GetUnderlyingType(propertyType);
        }

        return s ?? propertyType;
    }

    public static readonly HashSet<Type> EasyTypes =
        [typeof(string), typeof(int), typeof(double), typeof(bool)];

    [Pure, Obsolete]
    public static object ToTargetType(string value, Type type)
    {
        if (type == typeof(string))
            return value;

        if (type == typeof(int))
            return (int)double.Parse(value);

        if (type == typeof(double))
            return double.Parse(value);

        if (type == typeof(bool))
            return bool.Parse(value);

        return JsonSerializer.Deserialize(value, type);

    }
}