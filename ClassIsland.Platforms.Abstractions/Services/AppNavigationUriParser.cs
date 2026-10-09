namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 解析由操作系统转发给应用的 ClassIsland deep link。
/// </summary>
internal static class AppNavigationUriParser
{
    private static readonly HashSet<string> SafeAppNavigationRoots =
        new(StringComparer.Ordinal)
        {
            "settings",
            "profile",
            "live-activity",
            "helps"
        };

    public static bool TryParseClassIslandUri(string? value, out Uri? uri)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) ||
            !string.Equals(
                parsed.Scheme,
                "classisland",
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(parsed.Host, "app", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(parsed.UserInfo) ||
            !parsed.IsDefaultPort ||
            !IsSafeNavigationPath(parsed))
        {
            uri = null;
            return false;
        }

        uri = parsed;
        return true;
    }

    private static bool IsSafeNavigationPath(Uri uri)
    {
        string decodedPath;
        try
        {
            decodedPath = Uri.UnescapeDataString(uri.AbsolutePath);
        }
        catch (UriFormatException)
        {
            return false;
        }

        if (!string.Equals(decodedPath, uri.AbsolutePath, StringComparison.Ordinal) ||
            decodedPath.Contains('\\'))
        {
            return false;
        }

        var segments = decodedPath.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return segments.Length > 0 &&
               segments.All(segment => segment is not "." and not "..") &&
               SafeAppNavigationRoots.Contains(segments[0]);
    }
}
