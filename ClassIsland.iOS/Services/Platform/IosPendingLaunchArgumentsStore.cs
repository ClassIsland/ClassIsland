using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 在用户手动结束并重新打开应用之间保留一次性启动参数。
/// </summary>
internal static class IosPendingLaunchArgumentsStore
{
    private static readonly PendingLaunchArgumentsStore Store = new(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            "ClassIsland",
            ".pending-launch.json"));

    public static void Save(IReadOnlyList<string> arguments)
    {
        Store.Save(arguments);
    }

    public static string[] Consume()
    {
        return Store.Consume();
    }
}
