using System.Runtime.InteropServices;

namespace ClassIsland.Platforms.MacOs;

public static class API
{
    [DllImport(@"/System/Library/Frameworks/QuartzCore.framework/QuartzCore")]
    public static extern IntPtr CGWindowListCopyWindowInfo(CGWindowListOption option, uint relativeToWindow);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern void CFRelease(IntPtr cf);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool CGRectMakeWithDictionaryRepresentation(IntPtr dict, ref CGRect rect);
}