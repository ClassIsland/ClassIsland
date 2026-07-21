using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassIsland.Converters;
public class SupportedOSPlatformsToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var currentOsPlatform = Core.Enums.OSPlatform.Unknown;
        if (OperatingSystem.IsWindows())
        {
            currentOsPlatform = Core.Enums.OSPlatform.Windows;
        }
        else if (OperatingSystem.IsLinux())
        {
            currentOsPlatform = Core.Enums.OSPlatform.Linux;
        }
        else if (OperatingSystem.IsMacOS())
        {
            currentOsPlatform = Core.Enums.OSPlatform.macOS;
        }
        else if(OperatingSystem.IsAndroid())
        {
            currentOsPlatform = Core.Enums.OSPlatform.Android;
        }
        else if(OperatingSystem.IsIOS())
        {
            currentOsPlatform = Core.Enums.OSPlatform.iOS;
        }
        if (value is List<Core.Enums.OSPlatform> s)
        {
            return !s.Contains(currentOsPlatform);
        }
        return false;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}