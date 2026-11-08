using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 校验系统通知操作携带的日期和有效期，避免重复或过期的稍后提醒。
/// </summary>
internal static class IosNotificationActionPolicy
{
    internal const string SnoozedIdentifierPrefix = "classisland.snoozed.";
    internal static readonly TimeSpan SnoozeInterval = TimeSpan.FromMinutes(5);

    public static Uri CreateScheduleUri(string? date)
    {
        var query = DateOnly.TryParseExact(
            date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? $"?date={parsed:yyyy-MM-dd}"
            : "";
        return new Uri($"classisland://app/system/schedule{query}");
    }

    public static DateTimeOffset? GetSnoozeTime(
        DateTimeOffset now,
        string? expiresAtUnixSeconds,
        bool alreadySnoozed)
    {
        if (alreadySnoozed ||
            !long.TryParse(
                expiresAtUnixSeconds,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var seconds) ||
            seconds is < -62_135_596_800 or > 253_402_300_799 ||
            now > DateTimeOffset.MaxValue.Subtract(SnoozeInterval))
        {
            return null;
        }

        var fireAt = now.Add(SnoozeInterval);
        return fireAt < DateTimeOffset.FromUnixTimeSeconds(seconds)
            ? fireAt
            : null;
    }

    public static string? GetSnoozeIdentifier(string? identifier)
    {
        if (string.IsNullOrEmpty(identifier) ||
            identifier.Length > 512 ||
            !identifier.StartsWith("classisland.lessons.", StringComparison.Ordinal))
        {
            return null;
        }

        return SnoozedIdentifierPrefix +
               Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identifier)));
    }
}
