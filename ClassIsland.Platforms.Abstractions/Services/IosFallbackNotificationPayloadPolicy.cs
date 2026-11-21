namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 一条提醒请求中可用于 iOS 本地通知的文本。
/// </summary>
internal sealed record IosFallbackNotificationTextEntry(
    string? MaskText,
    string? OverlayText);

/// <summary>
/// iOS 即时本地通知的纯文本载荷。
/// </summary>
internal sealed record IosFallbackNotificationPayload(
    string Title,
    string Body);

/// <summary>
/// 将 ClassIsland 提醒内容折叠为 iOS 即时本地通知。
/// </summary>
internal static class IosFallbackNotificationPayloadPolicy
{
    private const string DefaultTitle = "ClassIsland 提醒";
    private const string DefaultBody = "你有一条新提醒。";

    public static IosFallbackNotificationPayload Create(
        string? providerName,
        IEnumerable<IosFallbackNotificationTextEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var normalizedEntries = entries
            .Select(x => new IosFallbackNotificationTextEntry(
                Normalize(x.MaskText),
                Normalize(x.OverlayText)))
            .ToArray();
        var normalizedProviderName = Normalize(providerName);
        var title = normalizedEntries
                        .Select(x => x.MaskText)
                        .FirstOrDefault(x => x is not null) ??
                    normalizedProviderName ??
                    normalizedEntries
                        .Select(x => x.OverlayText)
                        .FirstOrDefault(x => x is not null) ??
                    DefaultTitle;

        var bodyParts = new List<string>();
        var bodyTexts = new HashSet<string>(StringComparer.Ordinal) { title };
        foreach (var entry in normalizedEntries)
        {
            AddBodyPart(entry.MaskText);
            AddBodyPart(entry.OverlayText);
        }

        if (bodyParts.Count == 0)
        {
            if (normalizedProviderName is not null &&
                bodyTexts.Add(normalizedProviderName))
            {
                bodyParts.Add(normalizedProviderName);
            }
            else
            {
                bodyParts.Add(DefaultBody);
            }
        }

        return new IosFallbackNotificationPayload(
            title,
            string.Join(Environment.NewLine, bodyParts));

        void AddBodyPart(string? text)
        {
            if (text is not null && bodyTexts.Add(text))
            {
                bodyParts.Add(text);
            }
        }
    }

    public static bool ShouldPlaySound(
        bool allowNotificationSound,
        IEnumerable<bool> notificationSoundEnabledStates)
    {
        ArgumentNullException.ThrowIfNull(notificationSoundEnabledStates);
        return allowNotificationSound && notificationSoundEnabledStates.Any(x => x);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Replace("\0", "", StringComparison.Ordinal).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
