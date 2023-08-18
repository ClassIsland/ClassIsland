using System;

namespace ClassIsland;

public static class DateTimeToCurrentDateTimeConverter
{
    public static DateTime Convert(DateTime dateTime)
    {
        var now = DateTime.Now;
        return new DateTime(now.Year, now.Month, now.Day, dateTime.Hour, dateTime.Minute,
            dateTime.Second);
    }
}