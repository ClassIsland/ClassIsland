using System.Text.Json;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core;

public class AttachableSettingsObject : ObservableRecipient
{
    public Dictionary<string, object?> AttachedObjects { get; set; } = new();

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

    public void WriteAttachedObject<T>(Guid id, T o)
    {
        AttachedObjects[id.ToString()] = o;
    }

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