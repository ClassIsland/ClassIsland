using System;

namespace ClassIsland.Helpers;

public static class TimeSpanFormatHelper
{
    public static string Format(TimeSpan ts)
    {
        var r = "";
        var h = ts.TotalHours;
        var m = ts.Minutes;
        var s = ts.Seconds;

        if (h >= 1)
        {
            r += $"{Math.Floor(ts.TotalHours)}小时";
        }
        if (m >= 1)
        {
            if (s >= 1)
            {
                r += $"{m}分";
            }
            else
            {
                r += $"{m}分钟";
            }
        }
        if (s >= 1)
        {
            r += $"{s}秒";
        }

        return r;
    }
}