using System.Text.Json;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared;

/// <summary>
/// 代表一个可附加设置的对象
/// </summary>
public class AttachableSettingsObject : ObservableRecipient
{
    /// <summary>
    /// 已附加的设置。键是GUID。
    /// </summary>
    public Dictionary<string, object?> AttachedObjects { get; set; } = new();

    /// <summary>
    /// 获取指定的附加设置项。
    /// </summary>
    /// <typeparam name="T">要获取的附加设置类型</typeparam>
    /// <param name="id">要获取的附加设置ID</param>
    /// <returns>获取到的附加设置。如果没有找到附加设置，则返回null。</returns>
    public T? GetAttachedObject<T>(Guid id)
    {
        var key = id.ToString();
        var o = AttachedObjects.ContainsKey(key) ? AttachedObjects[key] : null;
        if (o is JsonElement o1)
        {
            return o1.Deserialize<T>();
        }
        return (T?)o;
    }
    /// <summary>
    /// 写入指定的附加设置。
    /// </summary>
    /// <typeparam name="T">要写入的附加设置类型</typeparam>
    /// <param name="id">要写入的附加设置ID</param>
    /// <param name="o">要写入的附加设置对象</param>
    public void WriteAttachedObject<T>(Guid id, T o)
    {
        AttachedObjects[id.ToString()] = o;
    }

    /// <summary>
    /// 获取指定的附加设置项，如果获取失败则将给定的默认值写入附加设置项，并返回给定的默认值。
    /// </summary>
    /// <typeparam name="T">要获取的附加设置类型</typeparam>
    /// <param name="id">要获取的附加设置ID</param>
    /// <param name="defaultValue">获取失败时返回的默认值</param>
    /// <returns>获取的附加设置项或默认值</returns>
    public T GetAttachedObject<T>(Guid id, T defaultValue)
    {
        var r = GetAttachedObject<T>(id);
        if (r != null)
        {
            WriteAttachedObject(id, r);
            return r;
        }
        WriteAttachedObject(id, defaultValue);
        return defaultValue;
    }
}