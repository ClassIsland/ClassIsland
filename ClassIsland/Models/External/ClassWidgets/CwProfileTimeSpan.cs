using System;

namespace ClassIsland.Models.External.ClassWidgets;

public class CwProfileTimeSpan
{
    public string Name { get; init; } = "";
    public TimeSpanType Type { get; init; } = TimeSpanType.Part;
    public TimeSpan StartTime { get; init; } = TimeSpan.Zero;

    public enum TimeSpanType
    {
        Part,
        Break
    }
}