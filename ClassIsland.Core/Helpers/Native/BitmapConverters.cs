using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ClassIsland.Core.Helpers.Native;

/// <summary>
/// Source: https://www.cnblogs.com/kybs0/p/15768990.html
/// </summary>
public class BitmapConveters
{
    [DllImport("gdi32")]
    static extern int DeleteObject(IntPtr o);
    public static BitmapSource ConvertToBitMapSource(Bitmap bitmap)
    {
        IntPtr intPtrl = bitmap.GetHbitmap();
        BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(intPtrl,
            IntPtr.Zero,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
        DeleteObject(intPtrl);
        return bitmapSource;
    }
    public static BitmapImage ConvertToBitmapImage(Bitmap bitmap, int? w=null, int? h=null)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            bitmap.Save(stream, ImageFormat.Png);
            stream.Position = 0;
            BitmapImage result = new BitmapImage();
            result.BeginInit();
            result.CacheOption = BitmapCacheOption.OnLoad;
            if (h != null && h<=bitmap.Height)
            {
                result.DecodePixelWidth = (int)((double)bitmap.Width / (double)bitmap.Height * (double)h);
                result.DecodePixelHeight = h.Value;
            }
            else if (w != null && w <= bitmap.Width)
            {
                result.DecodePixelHeight = (int)((double)bitmap.Height / (double)bitmap.Width * (double)w);
                result.DecodePixelWidth = w.Value;
            }
            else
            {
                result.DecodePixelWidth = bitmap.Width;
                result.DecodePixelHeight = bitmap.Height;
            }
            result.StreamSource = stream;
            result.EndInit();
            result.Freeze();
            return result;
        }
    }

    public static byte[] BitmapToByteArray(Bitmap bitmap)
    {
        var pixelFormat2 = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapdata = bitmap.LockBits(rect, ImageLockMode.ReadOnly, pixelFormat2);
        var length = Math.Abs(bitmapdata.Stride) * bitmap.Height;
        var destination = new byte[length];
        Marshal.Copy(bitmapdata.Scan0, destination, 0, length);
        bitmap.UnlockBits(bitmapdata);
        return destination;
    }
}