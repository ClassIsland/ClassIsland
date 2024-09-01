using System;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
namespace ClassIsland;

public static class DateTimeToCurrentDateTimeConverter
{
    public static DateTime Convert(DateTime dateTime)
    {
        var now = IAppHost.GetService<IExactTimeService>().GetCurrentLocalDateTime();
        return new DateTime(now.Year, now.Month, now.Day, dateTime.Hour, dateTime.Minute,
            dateTime.Second);
    }
}