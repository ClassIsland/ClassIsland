using System;

namespace ClassIsland;

public class NetworkSpeedFormater
{
    public static string FormatFileSize(long fileSize)
    {
        return fileSize switch
        {
            < 0 => throw new ArgumentOutOfRangeException("fileSize"),
            >= 1024 * 1024 * 1024 => $"{(double)fileSize / (1024 * 1024 * 1024):########0.00} GB",
            >= 1024 * 1024 => $"{(double)fileSize / (1024 * 1024):####0.00} MB",
            >= 1024 => $"{(double)fileSize / 1024:####0.00} KB",
            _ => $"{fileSize} bytes"
        };
    }
}