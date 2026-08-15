using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Data.Converters;
using Avalonia.Platform;
using Sentry.Protocol;

namespace ClassIsland.Converters;
public class SupportedOSPlatformsToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        OSPlatform currentOsPlatform = default;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            currentOsPlatform = OSPlatform.Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            currentOsPlatform = OSPlatform.Linux;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            currentOsPlatform = OSPlatform.OSX;
        }
        if (value is List<OSPlatform> s)
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