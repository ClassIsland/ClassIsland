using System.Globalization;

namespace ClassIsland.Platforms.Abstraction.Services;

internal sealed record IosSystemNavigationRequest(
    string Action,
    DateOnly? Date = null,
    Guid? ClassPlanId = null,
    string? LessonId = null,
    bool? NotificationsEnabled = null)
{
    public static bool TryParse(Uri uri, out IosSystemNavigationRequest? request)
    {
        request = null;
        if (!uri.IsAbsoluteUri || !AppNavigationUriParser.TryParseClassIslandUri(uri.AbsoluteUri, out _) ||
            !uri.AbsolutePath.StartsWith("/system/", StringComparison.Ordinal))
        {
            return false;
        }

        var action = uri.AbsolutePath[8..];
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2 || !values.TryAdd(Uri.UnescapeDataString(pair[0]), Uri.UnescapeDataString(pair[1])))
            {
                return false;
            }
        }

        DateOnly? date = null;
        if (values.TryGetValue("date", out var rawDate))
        {
            if (!DateOnly.TryParseExact(rawDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedDate))
            {
                return false;
            }
            date = parsedDate;
        }

        Guid? classPlanId = null;
        if (action is "class-plan" or "view-plan")
        {
            if (!values.TryGetValue("id", out var rawId) || !Guid.TryParse(rawId, out var parsedId) || parsedId == Guid.Empty)
            {
                return false;
            }
            classPlanId = parsedId;
        }

        bool? notificationsEnabled = null;
        if (action == "notifications")
        {
            if (!values.TryGetValue("enabled", out var rawEnabled) || !bool.TryParse(rawEnabled, out var enabled))
            {
                return false;
            }
            notificationsEnabled = enabled;
        }

        if (action is not ("schedule" or "today" or "previous-day" or "next-day" or
            "settings" or "profile" or "class-plan" or "view-plan" or "notifications"))
        {
            return false;
        }

        values.TryGetValue("lesson", out var lessonId);
        if (lessonId != null && (!int.TryParse(lessonId, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index < 0) &&
            !Guid.TryParse(lessonId, out _))
        {
            return false;
        }

        request = new IosSystemNavigationRequest(action, date, classPlanId, lessonId, notificationsEnabled);
        return true;
    }
}
