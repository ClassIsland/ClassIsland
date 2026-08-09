using System;
using System.Collections.Generic;
using System.Globalization;
using ClassIsland.Core.Models.Weather;

namespace ClassIsland.Services;

public enum SunTransition
{
    Sunrise,
    Sunset,
    Either
}

/// <summary>
/// 解析天气预报中的日出日落时间表。
/// </summary>
internal static class SunriseSunsetSchedule
{
    public static bool TryGetNextTransition(
        IReadOnlyList<RangedValue>? schedule,
        DateTimeOffset now,
        SunTransition transition,
        IReadOnlySet<DateOnly>? excludedForecastDates,
        out DateTimeOffset nextTransition)
    {
        nextTransition = default;
        DateTimeOffset? candidate = null;
        if (schedule == null)
        {
            return false;
        }

        foreach (var item in schedule)
        {
            if (transition is SunTransition.Sunrise or SunTransition.Either)
            {
                UpdateCandidate(item.From);
            }

            if (transition is SunTransition.Sunset or SunTransition.Either)
            {
                UpdateCandidate(item.To);
            }
        }

        if (candidate == null)
        {
            return false;
        }

        nextTransition = candidate.Value;
        return true;

        void UpdateCandidate(string value)
        {
            if (!TryParseTransition(value, out var parsed) || parsed <= now ||
                excludedForecastDates?.Contains(DateOnly.FromDateTime(parsed.Date)) == true ||
                candidate != null && parsed >= candidate.Value)
            {
                return;
            }

            candidate = parsed;
        }
    }

    public static bool TryGetDaylightStatus(
        IReadOnlyList<RangedValue>? schedule,
        DateTimeOffset now,
        out bool isDaylight)
    {
        isDaylight = false;
        if (schedule == null)
        {
            return false;
        }

        foreach (var item in schedule)
        {
            if (!TryParseTransition(item.From, out var sunrise) ||
                !TryParseTransition(item.To, out var sunset))
            {
                continue;
            }

            var isCurrentForecastDay = now.ToOffset(sunrise.Offset).Date == sunrise.Date ||
                                       now.ToOffset(sunset.Offset).Date == sunset.Date;
            if (!isCurrentForecastDay)
            {
                continue;
            }

            isDaylight = now >= sunrise && now < sunset;
            return true;
        }

        return false;
    }

    private static bool TryParseTransition(string value, out DateTimeOffset transition)
    {
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out transition);
    }
}
