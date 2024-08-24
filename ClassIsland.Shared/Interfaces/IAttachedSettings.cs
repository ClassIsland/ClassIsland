using System.Text.Json;

namespace ClassIsland.Shared.Interfaces;

/// <summary>
/// 附加设置接口
/// </summary>
public interface IAttachedSettings
{
    /// <summary>
    /// 此附加设置是否启用
    /// </summary>
    public bool IsAttachSettingsEnabled { 
        get; 
        set;
    }

#if !NETFRAMEWORK
    
    /// <summary>
    /// 判断指定的<see cref="IAttachedSettings"/>是否启用。
    /// </summary>
    /// <param name="obj">要判断的<see cref="IAttachedSettings"/></param>
    /// <returns>如果此<see cref="IAttachedSettings"/>启用，则返回true。</returns>
    public static bool GetIsEnabled(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        return obj switch
        {
            JsonElement json when json.TryGetProperty(nameof(IsAttachSettingsEnabled), out var element) =>
                element.ValueKind == JsonValueKind.True,
            IAttachedSettings settings => settings.IsAttachSettingsEnabled,
            _ => false
        };
    }
#endif
}