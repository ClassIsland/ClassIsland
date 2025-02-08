using System;

namespace ClassIsland.Models;

public record ScheduleClassPosition(DateTime Date, int Index)
{
    public static readonly ScheduleClassPosition Zero = new(DateTime.MinValue, -1);

    public override string ToString()
    {
        return $"{Date:yy/MM/dd ddd} 第{Index + 1}节课";
    }
}