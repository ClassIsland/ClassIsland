using System;
using ClassIsland.Services;
using ClassIsland.Shared;
namespace ClassIsland;

public static class DateTimeToCurrentDateTimeConverter
{
    public static DateTime Convert(DateTime dateTime)
    {
        var now = IAppHost.GetService<ExactTimeService>().GetCurrentLocalDateTime();
        return new DateTime(now.Year, now.Month, now.Day, dateTime.Hour, dateTime.Minute,
            dateTime.Second);
    }
}