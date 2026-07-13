namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 解析由操作系统转发给应用的 ClassIsland deep link。
/// </summary>
internal static class AppNavigationUriParser
{
    public static bool TryParseClassIslandUri(string? value, out Uri? uri)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) ||
            !string.Equals(
                parsed.Scheme,
                "classisland",
                StringComparison.OrdinalIgnoreCase))
        {
            uri = null;
            return false;
        }

        uri = parsed;
        return true;
    }
}
