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
                (candidate != null && parsed >= candidate.Value))
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
        return TryGetDaylightStatus(schedule, now, useSystemLocalDate: false, out isDaylight);
    }

    public static bool TryGetDaylightStatusForSystemLocalDate(
        IReadOnlyList<RangedValue>? schedule,
        DateTimeOffset now,
        out bool isDaylight)
    {
        return TryGetDaylightStatus(schedule, now, useSystemLocalDate: true, out isDaylight);
    }

    private static bool TryGetDaylightStatus(
        IReadOnlyList<RangedValue>? schedule,
        DateTimeOffset now,
        bool useSystemLocalDate,
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

            var isCurrentForecastDay = useSystemLocalDate
                ? now.Date == sunrise.Date || now.Date == sunset.Date
                : now.ToOffset(sunrise.Offset).Date == sunrise.Date ||
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

    public static bool TryGetLatestTransitionAtOrBefore(
        IReadOnlyList<RangedValue>? schedule,
        SunTransition transition,
        DateOnly earliestForecastDate,
        DateTimeOffset atOrBefore,
        IReadOnlySet<DateOnly>? excludedForecastDates,
        out DateTimeOffset latestTransition)
    {
        return TryGetLatestTransition(
            schedule,
            transition,
            parsed => parsed <= atOrBefore &&
                      DateOnly.FromDateTime(parsed.Date) >= earliestForecastDate,
            excludedForecastDates,
            out latestTransition);
    }

    public static bool TryGetLatestTransitionBetween(
        IReadOnlyList<RangedValue>? schedule,
        SunTransition transition,
        DateTimeOffset afterExclusive,
        DateTimeOffset atOrBefore,
        IReadOnlySet<DateOnly>? excludedForecastDates,
        out DateTimeOffset latestTransition)
    {
        return TryGetLatestTransition(
            schedule,
            transition,
            parsed => parsed > afterExclusive && parsed <= atOrBefore,
            excludedForecastDates,
            out latestTransition);
    }

    private static bool TryParseTransition(string value, out DateTimeOffset transition)
    {
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out transition);
    }

    private static bool TryGetLatestTransition(
        IReadOnlyList<RangedValue>? schedule,
        SunTransition transition,
        Func<DateTimeOffset, bool> isInRange,
        IReadOnlySet<DateOnly>? excludedForecastDates,
        out DateTimeOffset latestTransition)
    {
        latestTransition = default;
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

        latestTransition = candidate.Value;
        return true;

        void UpdateCandidate(string value)
        {
            if (!TryParseTransition(value, out var parsed))
            {
                return;
            }

            var forecastDate = DateOnly.FromDateTime(parsed.Date);
            if (!isInRange(parsed) ||
                excludedForecastDates?.Contains(forecastDate) == true ||
                (candidate != null && parsed <= candidate.Value))
            {
                return;
            }

            candidate = parsed;
        }
    }
}
