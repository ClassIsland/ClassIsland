using System;
using System.Globalization;
using System.Net.Cache;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ClassIsland.Converters;

public class WebImageUrlToBitmapImageConverter : IValueConverter
{
    public int? DecodePixelWidth { get; set; }
    public int? DecodePixelHeight { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var url = (string)value;
        var img = BitmapFrame.Create(new Uri(url, UriKind.RelativeOrAbsolute), 
            BitmapCreateOptions.DelayCreation, 
            BitmapCacheOption.Default, 
            new RequestCachePolicy(RequestCacheLevel.BypassCache));
        return img;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}