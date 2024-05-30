using System.Text.Json;

namespace ClassIsland.Core.Helpers;

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
            if (backupEnabled.Value)
                return LoadConfig<T>(path + ".bak", false, true);
            throw;
        }

    }

    public static void SaveConfig<T>(string path, T o)
    {
        // 备份原文件
        if (File.Exists(path + ".bak"))
        {
            File.Delete(path + ".bak");
        }
        File.Copy(path, path + ".bak");
        File.WriteAllText(path + ".bak", JsonSerializer.Serialize<T>(o));
        // 校验备份文件是否写入成功
        var bakInfo = new FileInfo(path + ".bak");
        // 由于默认的Profile实际大小为15735，因此如果写入后文件大小小于15700则判断写入失败
        if (bakInfo.Length <= 15700)
        {
            File.Delete(path + ".bak");
            return;
        }
        File.WriteAllText(path, JsonSerializer.Serialize<T>(o));
        // 校验写入结果
        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > 15700)
        {
            File.Delete(path + ".bak");
            return;
        }
        
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