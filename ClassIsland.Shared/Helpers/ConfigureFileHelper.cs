using System.Text;
using System.Text.Json;

namespace ClassIsland.Shared.Helpers;

/// <summary>
/// 配置文件工具集
/// </summary>
public class ConfigureFileHelper
{
    /// <summary>
    /// 配置在默认情况下，是否启用配置文件备份
    /// </summary>
    public static bool IsBackupEnabled { get; set; } = true;

    /// <summary>
    /// 加载配置文件，并自动创建备份。当加载失败时，将尝试加载备份的配置文件。
    /// </summary>
    /// <typeparam name="T">配置文件类型</typeparam>
    /// <param name="path">配置文件路径</param>
    /// <param name="backupEnabled">是否启用备份。如果没有填写此参数，将使用默认值。</param>
    /// <param name="isLoadingBackup">是否正在加载备份。如果是，在加载失败时将不会尝试加载备份。</param>
    /// <returns>加载的配置文件对象</returns>
    public static T LoadConfig<T>(string path, bool? backupEnabled=null, bool isLoadingBackup=false)
    {
        backupEnabled ??= IsBackupEnabled;
        if (!File.Exists(path))
        {
            var cfg = Activator.CreateInstance<T>();
            if (!isLoadingBackup)
                SaveConfig(path, cfg);
            return cfg;
        }

        try
        {
            var json = File.ReadAllText(path);
            var r = JsonSerializer.Deserialize<T>(json);
            if (r == null) 
                return Activator.CreateInstance<T>();
            if (backupEnabled.Value)
                File.Copy(path, path + ".bak", true);
            return r;
        }
        catch (Exception ex)
        {
            if (!backupEnabled.Value)
                throw;
            var r = LoadConfig<T>(path + ".bak", false, true);
            File.Copy(path + ".bak", path, true);
            return r;
        }

    }

    /// <summary>
    /// 保存配置文件，并自动创建备份
    /// </summary>
    /// <typeparam name="T">配置文件类型</typeparam>
    /// <param name="path">配置文件路径</param>
    /// <param name="o">要写入到配置的对象</param>
    public static void SaveConfig<T>(string path, T o)
    {
        // 在保存时不对备份文件进行操作，以防止在保存时发生意外断电时，备份文件也受到损坏。
        WriteAllTextSafe(path, JsonSerializer.Serialize<T>(o));
    }

    /// <summary>
    /// 复制配置对象
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="o">要复制的对象</param>
    /// <returns>复制后的对象副本</returns>
    public static T CopyObject<T>(T o)
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(o)) ?? Activator.CreateInstance<T>();
    }

    /// <summary>
    /// 合并字典。将新的字典与基准字典比较，向要操作的字典中添加新字典与基准字典相较没有的元素，并删除要操作的字典中新字典与基准字典相比没有的元素。
    /// </summary>
    /// <typeparam name="TKey">字典键类型</typeparam>
    /// <typeparam name="TValue">字典值类型</typeparam>
    /// <param name="raw">要操作的字典</param>
    /// <param name="diffBase">合并基准字典</param>
    /// <param name="diffNew">新的字典</param>
    /// <returns></returns>
    public static IDictionary<TKey, TValue> MergeDictionary<TKey, TValue>(IDictionary<TKey, TValue> raw,
        IDictionary<TKey, TValue> diffBase, IDictionary<TKey, TValue> diffNew)
    {
        var rm = (from i in diffNew.Keys where !diffBase.Keys.Contains(i) select i).ToList();
        rm.ForEach(i => raw.Remove(i));
        foreach (var i in diffNew)
        {
            if (raw.ContainsKey(i.Key))
                raw[i.Key] = i.Value;
            else
                raw.Add(i);
        }

        return raw;
    }

    private static void WriteAllTextSafe(string path, string content)
    {
        using var stream = new FileStream(path, FileMode.Create);
        using var writer = new StreamWriter(stream);
        writer.Write(content);
        stream.Flush(true);
    }
}