using System;
using System.Globalization;

namespace ClassIsland.Services.Management;

public static class BashuNotificationTiming
{
    public static TimeSpan Duration(string content, string author, int repeat)
    {
        // Budget for a complete readable pass, including sender and punctuation pauses.
        var characters = new StringInfo(content).LengthInTextElements;
        var punctuation = 0;
        foreach (var c in content) if (char.IsPunctuation(c) || c == '\n') punctuation++;
        var seconds = Math.Max(8, (characters + author.Length + 8) / 3.0 + punctuation * 0.35 + 4);
        return TimeSpan.FromSeconds(seconds * Math.Clamp(repeat, 1, 10));
    }
}
