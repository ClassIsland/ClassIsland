using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Helpers;

namespace ClassIsland.Core.Services;

/// <summary>
/// 应用机器范围全局静态存储服务
/// </summary>
public static class GlobalStorageService
{
    private static bool _isInitialized = false;
    private static ObservableDictionary<string, string> _storage = new();
    private static readonly string GlobalStoragePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClassIsland", "Global",
            "Global.json");

    /// <summary>
    /// 数据存储字典
    /// </summary>
    public static ObservableDictionary<string, string> Storage
    {
        get
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("全局存储尚未初始化，不能调用全局存储");
            }
            return _storage;
        }
    }

    /// <summary>
    /// 获取指定的存储值
    /// </summary>
    /// <param name="key">存储键名</param>
    /// <returns>返回对应的值。如果值为空，，则返回 null</returns>
    [Pure]
    public static string? GetValue(string key)
    {
        return Storage.GetValueOrDefault(key);
    }

    /// <summary>
    /// 设置指定的存储值
    /// </summary>
    /// <param name="key">存储键名</param>
    /// <param name="value">要设置的值。如果为 null，将删除对应的值。</param>
    public static void SetValue(string key, string? value)
    {
        if (value == null)
        {
            Storage.Remove(key);
            return;
        }
        Storage[key] = value;
    }

    internal static void InitializeGlobalStorage()
    {
        var dirPath = Path.GetDirectoryName(GlobalStoragePath);
        if (!Directory.Exists(dirPath) && dirPath != null)
        {
            Directory.CreateDirectory(dirPath);
        }

        _storage = ConfigureFileHelper.LoadConfig<ObservableDictionary<string, string>>(GlobalStoragePath);
        _storage.CollectionChanged += StorageOnCollectionChanged;
        _isInitialized = true;
    }

    private static void StorageOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ConfigureFileHelper.SaveConfig(GlobalStoragePath, _storage);
    }
}