#if MACOS
using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace ClassIsland.Helpers
{
    public static class MacWindowHelper
    {
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
        public static extern IntPtr sel_registerName(string selectorName);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern void objc_msgSend_void(IntPtr receiver, IntPtr selector, IntPtr arg);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern void objc_msgSend_bool(IntPtr receiver, IntPtr selector, bool arg);

        public static void CustomizeMacWindow(Window window)
        {
            var handle = window.PlatformImpl?.Handle?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero) return;

            // 1. titleVisibility = .hidden
            var selTitleVisibility = sel_registerName("setTitleVisibility:");
            var NSWindowTitleHidden = (IntPtr)1; // NSWindowTitleVisibilityHidden = 1
            objc_msgSend_void(handle, selTitleVisibility, NSWindowTitleHidden);

            // 2. titlebarAppearsTransparent = true
            var selTitlebarAppearsTransparent = sel_registerName("setTitlebarAppearsTransparent:");
            objc_msgSend_bool(handle, selTitlebarAppearsTransparent, true);

            // 3. styleMask.insert(.fullSizeContentView)
            var selStyleMask = sel_registerName("styleMask");
            var styleMask = objc_msgSend(handle, selStyleMask);
            var NSWindowStyleMaskFullSizeContentView = (IntPtr)(1 << 15);
            styleMask = (IntPtr)(styleMask.ToInt64() | NSWindowStyleMaskFullSizeContentView.ToInt64());
            var selSetStyleMask = sel_registerName("setStyleMask:");
            objc_msgSend_void(handle, selSetStyleMask, styleMask);
        }
    }
}
#endif
