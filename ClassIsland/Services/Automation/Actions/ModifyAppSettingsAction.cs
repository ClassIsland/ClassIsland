using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.Actions;
using Microsoft.Extensions.Logging;
namespace ClassIsland.Services.Automation.Actions;

[ActionInfo("classisland.settings", "应用设置", "\uef27", addDefaultToMenu: false)]
public class ModifyAppSettingsAction : ActionBase<ModifyAppSettingsActionSettings>
{
    SettingsService SettingsService { get; } = App.GetService<SettingsService>();
    ILogger<ModifyAppSettingsActionSettings> Logger { get; } = App.GetService<ILogger<ModifyAppSettingsActionSettings>>();

    protected override async Task OnInvoke()
    {
        base.OnInvoke();

        if (Settings.Name == "") return;

        var propertyInfo = SettingsService.GetPropertyInfoByName(Settings.Name);
        if (propertyInfo == null)
            throw new KeyNotFoundException($"应用设置中找不到属性“{Settings.Name}”。");

        if (Settings.Value == null)
            throw new ArgumentNullException(nameof(Settings.Name));

        var value = ConvertToAssignableToSettingsType(Settings.Value, propertyInfo.PropertyType);

        if (value == null)
            throw new ArgumentNullException(Settings.Value.ToString(), Settings.Value.GetType().ToString());

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (IsRevertable)
                SettingsService.AddSettingsOverlay(ActionSet.Guid, Settings.Name, value);
            else
                propertyInfo.SetValue(SettingsService.Settings, value);
        });
    }

    protected override async Task OnRevert()
    {
        base.OnRevert();

        if (Settings.Name == "") return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SettingsService.RemoveSettingsOverlay(ActionSet.Guid, Settings.Name);
        });
    }

    /// 转换为可赋值给运行时属性的类型。
    public static object? ConvertToAssignableToSettingsType(object? value, Type type)
    {
        if (value == null) return null;

        if (value.GetType() == type)
            return value;

        if (IsTypeSupported(type))
        {
            if (value is JsonElement jsonElement)
                return jsonElement.Deserialize(type, FriendlyJsonSerializerOptions);
            else
                return Convert.ChangeType(value, type);
        }
        else
        {
            if (value is JsonElement json)
            {
                return json.Deserialize(type, FriendlyJsonSerializerOptions);
            }

            if (value is string str)
                return JsonSerializer.Deserialize(str, type, FriendlyJsonSerializerOptions);

            return Convert.ChangeType(value, type);
        }
    }

    [Pure] public static bool IsTypeSupported(Type type)
    {
        type = GetUnderlyingType(type);
        return SupportedTypes.Contains(type) || type.IsEnum;
    }

    [Pure] public static Type GetUnderlyingType(Type type) =>
        Nullable.GetUnderlyingType(type) ?? type;

    public static readonly JsonSerializerOptions FriendlyJsonSerializerOptions = new()
    {
        AllowOutOfOrderMetadataProperties = true,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
    };

    public static readonly HashSet<Type> SupportedTypes =
        [typeof(string), typeof(int), typeof(double), typeof(bool), typeof(Color)];
}