using ClassIsland.Platforms.Abstraction.Services;
using Foundation;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 在用户手动结束并重新打开应用之间保留一次性启动参数。
/// </summary>
internal static class IosPendingLaunchArgumentsStore
{
    private static readonly string LegacyStorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Personal),
        "ClassIsland",
        ".pending-launch.json");

    private static readonly PendingLaunchArgumentsStore Store = new(
        GetStorePath(),
        timeToLive: TimeSpan.FromMinutes(30));

    public static void Save(IReadOnlyList<string> arguments)
    {
        DeleteLegacyStore();
        Store.Save(arguments);
    }

    public static string[] Consume()
    {
        DeleteLegacyStore();
        return Store.Consume();
    }

    private static void DeleteLegacyStore()
    {
        try
        {
            File.Delete(LegacyStorePath);
        }
        catch
        {
            // 旧版残留清理失败不应阻止启动或保存新的待处理参数。
        }
    }

    private static string GetStorePath()
    {
        using var applicationSupportUrl = NSFileManager.DefaultManager
            .GetUrls(
                NSSearchPathDirectory.ApplicationSupportDirectory,
                NSSearchPathDomain.User)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                "无法获取 iOS Application Support 目录。");
        var applicationSupportPath = applicationSupportUrl.Path
            ?? throw new InvalidOperationException(
                "无法获取 iOS Application Support 目录路径。");
        return Path.Combine(
            applicationSupportPath,
            "ClassIsland",
            ".pending-launch.json");
    }
}
