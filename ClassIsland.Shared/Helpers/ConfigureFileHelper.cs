using System.Text.Json;

namespace ClassIsland.Shared.Helpers;

public class ConfigureFileHelper
{
    public static bool IsBackupEnabled { get; set; } = true;

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

    public static void SaveConfig<T>(string path, T o)
    {
        // 备份原文件
        if (File.Exists(path))
        {
            File.Copy(path, path + ".bak", true);
        }
        else
        {
            File.WriteAllText(path + ".bak", JsonSerializer.Serialize<T>(o));
            // 校验备份文件是否写入成功
            var bakInfo = new FileInfo(path + ".bak");
            if (bakInfo.Length <= 0)
            {
                File.Delete(path + ".bak");
                return;
            }
        }
        File.WriteAllText(path, JsonSerializer.Serialize<T>(o));
    }

    public static T CopyObject<T>(T o)
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(o)) ?? Activator.CreateInstance<T>();
    }

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
}