namespace ClassIsland.Platforms.Abstraction.Models.LiveActivities;

/// <summary>
/// 优先保留基础课程内容，在系统载荷预算内缩短组件文本和移除可选图标。
/// </summary>
internal static class LessonLiveActivityPayloadBudget
{
    public static LessonLiveActivityContent Fit(
        LessonLiveActivityContent content,
        Func<LessonLiveActivityContent, int> measureBytes,
        int maximumBytes)
    {
        if (measureBytes(content) <= maximumBytes)
        {
            return content;
        }

        if (content.Details?.IconPngBase64 != null)
        {
            content = content with { Details = content.Details with { IconPngBase64 = null } };
        }

        foreach (var length in new[] { 24, 12, 0 })
        {
            if (measureBytes(content) <= maximumBytes || content.Components == null)
            {
                return content;
            }

            content = content with
            {
                Components = content.Components.Select(component => component with
                {
                    Text = Shorten(component.Text, Math.Max(12, length)),
                    SecondaryText = Shorten(component.SecondaryText, length),
                    ExtraText = Shorten(component.ExtraText, length)
                }).ToArray()
            };
        }

        while (measureBytes(content) > maximumBytes && content.Components is { Length: > 0 })
        {
            content = content with { Components = content.Components[..^1] };
        }

        return content;
    }

    private static string Shorten(string value, int maximumElements)
    {
        if (maximumElements == 0)
        {
            return string.Empty;
        }

        var elements = System.Globalization.StringInfo.ParseCombiningCharacters(value);
        return elements.Length <= maximumElements
            ? value
            : value[..elements[maximumElements - 1]] + "…";
    }
}
